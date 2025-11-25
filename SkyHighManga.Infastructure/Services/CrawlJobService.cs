using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;
using LogLevel = SkyHighManga.Application.Common.Models.LogLevel;

namespace SkyHighManga.Infastructure.Services;

/// <summary>
/// Service để quản lý và thực thi crawl jobs với multi-tasking support
/// </summary>
public class CrawlJobService : ICrawlJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly IMangaService _mangaService;

    public CrawlJobService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        ICrawlerFactory crawlerFactory,
        IMangaService mangaService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _crawlerFactory = crawlerFactory;
        _mangaService = mangaService;
    }

    /// <summary>
    /// Crawl toàn bộ mangas từ search page với parallel processing tối ưu
    /// Flow: Xác định total pages -> Multi-task crawl list manga -> Multi-task crawl manga details (chapters) -> Multi-task crawl chapter pages
    /// </summary>
    public async Task<CrawlerResult<int>> CrawlAllMangasAsync(
        Source source,
        CrawlJob? crawlJob = null,
        int? maxPages = null,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        CrawlJob? job = null;

        try
        {
            // Tạo hoặc sử dụng job hiện có
            if (crawlJob == null)
            {
                job = await CreateCrawlJobAsync(
                    source,
                    CrawlJobType.FullCrawl,
                    "Crawl toàn bộ mangas từ search page",
                    $"{source.BaseUrl}/tim-kiem",
                    maxPages,
                    cancellationToken);
            }
            else
            {
                job = crawlJob;
                await UpdateJobStatusAsync(job, CrawlJobStatus.Running, cancellationToken);
            }

            var mangaCrawler = _crawlerFactory.CreateMangaCrawler(source);
            var chapterCrawler = _crawlerFactory.CreateChapterCrawler(source);
            var pageCrawler = _crawlerFactory.CreatePageCrawler(source);

            if (mangaCrawler == null || chapterCrawler == null || pageCrawler == null)
            {
                throw new InvalidOperationException("Không thể tạo crawler cho source này");
            }

            LogJob(job, "Đang xác định tổng số pages...", LogLevel.Info, cancellationToken);
            
            var baseSearchUrl = $"{source.BaseUrl}/tim-kiem";
            var firstPageContext = CreateCrawlerContext(source, job, cancellationToken);
            firstPageContext.StartUrl = baseSearchUrl;
            
            // Crawl trang đầu tiên để lấy tổng số pages (NettruyenCrawler parse từ pagination)
            var firstPageResult = await mangaCrawler.SearchMangaAsync("", firstPageContext, maxResults: 1, maxPages: 1);
            if (!firstPageResult.IsSuccess)
            {
                throw new Exception($"Lỗi khi crawl trang đầu tiên: {firstPageResult.ErrorMessage}");
            }
            
            // Xác định số pages cần crawl
            // Note: NettruyenCrawler.SearchMangaAsync đã parse totalPages và log ra
            // maxPages = null → crawl TẤT CẢ pages (tất cả pages của truyện)
            // maxPages > 0 → crawl tối đa maxPages pages
            // Sẽ truyền trực tiếp maxPages vào SearchMangaAsync, nó sẽ tự xử lý
            int? pagesToCrawl = maxPages; // null = crawl all pages, > 0 = crawl tối đa số pages đó
            
            if (pagesToCrawl == null)
            {
                LogJob(job, "maxPages = null → sẽ crawl TẤT CẢ pages của truyện", LogLevel.Info, cancellationToken);
            }
            else
            {
                LogJob(job, $"Sẽ crawl tối đa {pagesToCrawl} pages", LogLevel.Info, cancellationToken);
            }
            
            // Cấu hình concurrency - tăng để crawl nhanh hơn
            int mangaCrawlConcurrency = Math.Max(maxConcurrency, 10); // Tối thiểu 10 manga crawl đồng thời
            
            // Channel để pipeline data: pages -> manga list -> manga details
            var mangaChannel = Channel.CreateUnbounded<MangaCrawlData>();
            var mangaWriter = mangaChannel.Writer;
            var mangaReader = mangaChannel.Reader;
            
            var successCount = 0;
            var failedCount = 0;
            var processedCount = 0;
            var totalMangas = 0;
            var lockObject = new object();
            
            // Task để crawl manga details từ channel (multi-task)
            var mangaCrawlTask = Task.Run(async () =>
            {
                var mangaSemaphore = new SemaphoreSlim(mangaCrawlConcurrency, mangaCrawlConcurrency);
                var mangaTasks = new List<Task>();
                
                await foreach (var mangaData in mangaReader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    var manga = mangaData;
                    var task = Task.Run(async () =>
                    {
                        await mangaSemaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var mangaResult = await CrawlMangaFullInternalAsync(
                                source,
                                manga.SourceUrl,
                                job,
                                skipExisting: true,
                                mangaCrawler,
                                chapterCrawler,
                                pageCrawler,
                                cancellationToken);
                            
                            lock (lockObject)
                            {
                                processedCount++;
                                if (mangaResult.IsSuccess)
                                {
                                    successCount++;
                                }
                                else
                                {
                                    failedCount++;
                                }
                                
                                // Update progress mỗi 10 items
                                if (processedCount % 10 == 0)
                                {
                                    _ = UpdateJobProgressAsync(job, processedCount, successCount, failedCount, cancellationToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (lockObject)
                            {
                                processedCount++;
                                failedCount++;
                                LogJob(job, $"Lỗi khi crawl manga {manga.Title}: {ex.Message}", LogLevel.Error, cancellationToken, ex);
                            }
                        }
                        finally
                        {
                            mangaSemaphore.Release();
                        }
                    }, cancellationToken);
                    
                    mangaTasks.Add(task);
                }
                
                await Task.WhenAll(mangaTasks);
            }, cancellationToken);
            
            // Bước 2: Multi-task crawl list manga từ các pages
            // Sử dụng SearchMangaAsync với maxPages để crawl nhiều pages song song
            // NettruyenCrawler đã có parallel processing cho pages
            var allPagesContext = CreateCrawlerContext(source, job, cancellationToken);
            allPagesContext.StartUrl = baseSearchUrl;
            
            if (pagesToCrawl == null)
            {
                LogJob(job, "Bắt đầu crawl TẤT CẢ pages song song (maxPages = null)...", LogLevel.Info, cancellationToken);
            }
            else
            {
                LogJob(job, $"Bắt đầu crawl {pagesToCrawl} pages song song...", LogLevel.Info, cancellationToken);
            }
            
            var allPagesResult = await mangaCrawler.SearchMangaAsync(
                "",
                allPagesContext,
                maxResults: null,
                maxPages: pagesToCrawl); // null = crawl all, > 0 = crawl tối đa số pages đó
            
            if (allPagesResult.IsSuccess && allPagesResult.Data != null)
            {
                var allMangas = allPagesResult.Data.ToList();
                
                // Gửi tất cả mangas vào channel để crawl details
                foreach (var manga in allMangas)
                {
                    await mangaWriter.WriteAsync(manga, cancellationToken);
                    lock (lockObject)
                    {
                        totalMangas++;
                    }
                }
                
                LogJob(job, $"Đã crawl {pagesToCrawl} pages, tìm thấy {totalMangas} mangas", LogLevel.Info, cancellationToken);
            }
            else
            {
                LogJob(job, $"Cảnh báo: Không thể crawl tất cả pages: {allPagesResult.ErrorMessage}", LogLevel.Warning, cancellationToken);
            }
            
            // Đóng channel để signal không còn mangas nào nữa
            mangaWriter.Complete();
            
            // Đợi tất cả manga details crawl xong
            await mangaCrawlTask;
            
            job.TotalItems = totalMangas;

            stopwatch.Stop();
            job.ProcessedItems = processedCount;
            job.SuccessItems = successCount;
            job.FailedItems = failedCount;
            job.CompletedAt = DateTime.UtcNow;
            job.Duration = stopwatch.Elapsed;
            job.Status = CrawlJobStatus.Completed;
            job.UpdatedAt = DateTime.UtcNow;

            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            LogJob(job, $"Hoàn thành crawl {successCount}/{totalMangas} mangas thành công", LogLevel.Info, cancellationToken);

            return CrawlerResult<int>.Success(successCount);
        }
        catch (Exception ex)
        {
            if (job != null)
            {
                job.Status = CrawlJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.StackTrace = ex.StackTrace;
                job.CompletedAt = DateTime.UtcNow;
                job.Duration = stopwatch.Elapsed;
                job.UpdatedAt = DateTime.UtcNow;

                await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
                try
                {
                    _unitOfWork.CrawlJobs.Update(job);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                finally
                {
                    SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
                }

                LogJob(job, $"Job thất bại: {ex.Message}", LogLevel.Critical, cancellationToken, ex);
            }

            return CrawlerResult<int>.Failure($"Lỗi khi crawl: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Crawl một manga đầy đủ: Manga -> Chapters -> Pages
    /// </summary>
    public async Task<CrawlerResult<Guid>> CrawlMangaFullAsync(
        Source source,
        string mangaUrl,
        CrawlJob? crawlJob = null,
        bool skipExisting = true,
        CancellationToken cancellationToken = default)
    {
        var mangaCrawler = _crawlerFactory.CreateMangaCrawler(source);
        var chapterCrawler = _crawlerFactory.CreateChapterCrawler(source);
        var pageCrawler = _crawlerFactory.CreatePageCrawler(source);

        if (mangaCrawler == null || chapterCrawler == null || pageCrawler == null)
        {
            return CrawlerResult<Guid>.Failure("Không thể tạo crawler cho source này");
        }

        CrawlJob? job = crawlJob;
        if (job == null)
        {
            job = await CreateCrawlJobAsync(
                source,
                CrawlJobType.SingleManga,
                $"Crawl manga: {mangaUrl}",
                mangaUrl,
                null,
                cancellationToken);
        }

        try
        {
            await UpdateJobStatusAsync(job, CrawlJobStatus.Running, cancellationToken);

            var result = await CrawlMangaFullInternalAsync(
                source,
                mangaUrl,
                job,
                skipExisting,
                mangaCrawler,
                chapterCrawler,
                pageCrawler,
                cancellationToken);

            if (result.IsSuccess)
            {
                job.Status = CrawlJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                job.Status = CrawlJobStatus.Failed;
                job.ErrorMessage = result.ErrorMessage;
            }

            job.UpdatedAt = DateTime.UtcNow;
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            return result;
        }
        catch (Exception ex)
        {
            job.Status = CrawlJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.StackTrace = ex.StackTrace;
            job.UpdatedAt = DateTime.UtcNow;
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            return CrawlerResult<Guid>.Failure($"Lỗi khi crawl manga: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Crawl chapters cho một manga
    /// </summary>
    public async Task<CrawlerResult<int>> CrawlMangaChaptersAsync(
        Source source,
        Guid mangaId,
        CrawlJob? crawlJob = null,
        bool skipExisting = true,
        CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        Manga? manga;
        try
        {
            manga = await _unitOfWork.Mangas.GetByIdAsync(mangaId, cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
        
        if (manga == null)
        {
            return CrawlerResult<int>.Failure("Không tìm thấy manga");
        }

        var chapterCrawler = _crawlerFactory.CreateChapterCrawler(source);
        if (chapterCrawler == null)
        {
            return CrawlerResult<int>.Failure("Không thể tạo chapter crawler");
        }

        CrawlJob? job = crawlJob;
        if (job == null)
        {
            job = await CreateCrawlJobAsync(
                source,
                CrawlJobType.UpdateManga,
                $"Crawl chapters cho manga: {manga.Title}",
                manga.SourceUrl,
                null,
                cancellationToken);
            job.MangaId = mangaId;
        }

        try
        {
            await UpdateJobStatusAsync(job, CrawlJobStatus.Running, cancellationToken);

            var context = CreateCrawlerContext(source, job, cancellationToken);
            var chaptersResult = await chapterCrawler.CrawlChaptersAsync(
                manga.SourceUrl,
                context);

            if (!chaptersResult.IsSuccess || chaptersResult.Data == null)
            {
                throw new Exception($"Lỗi khi crawl chapters: {chaptersResult.ErrorMessage}");
            }

            var chapters = chaptersResult.Data.ToList();
            var savedCount = 0;

            foreach (var chapterData in chapters)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (skipExisting && await _mangaService.ChapterExistsAsync(mangaId, chapterData.SourceChapterId ?? "", cancellationToken))
                {
                    continue;
                }

                await _mangaService.SaveOrUpdateChapterAsync(chapterData, mangaId, cancellationToken);
                savedCount++;
            }

            job.Status = CrawlJobStatus.Completed;
            job.SuccessItems = savedCount;
            job.ProcessedItems = chapters.Count;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            return CrawlerResult<int>.Success(savedCount);
        }
        catch (Exception ex)
        {
            job.Status = CrawlJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.StackTrace = ex.StackTrace;
            job.UpdatedAt = DateTime.UtcNow;
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            return CrawlerResult<int>.Failure($"Lỗi khi crawl chapters: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Crawl pages cho một chapter
    /// </summary>
    public async Task<CrawlerResult<int>> CrawlChapterPagesAsync(
        Source source,
        Guid chapterId,
        CrawlJob? crawlJob = null,
        bool skipExisting = true,
        CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        Chapter? chapter;
        try
        {
            chapter = await _unitOfWork.Chapters.GetByIdAsync(chapterId, cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
        
        if (chapter == null)
        {
            return CrawlerResult<int>.Failure("Không tìm thấy chapter");
        }

        var pageCrawler = _crawlerFactory.CreatePageCrawler(source);
        if (pageCrawler == null)
        {
            return CrawlerResult<int>.Failure("Không thể tạo page crawler");
        }

        CrawlJob? job = crawlJob;
        if (job == null)
        {
            job = await CreateCrawlJobAsync(
                source,
                CrawlJobType.SingleManga,
                $"Crawl pages cho chapter: {chapter.Title}",
                chapter.SourceUrl,
                null,
                cancellationToken);
        }

        try
        {
            await UpdateJobStatusAsync(job, CrawlJobStatus.Running, cancellationToken);

            var context = CreateCrawlerContext(source, job, cancellationToken);
            var pagesResult = await pageCrawler.CrawlPageUrlsAsync(chapter.SourceUrl, context);

            if (!pagesResult.IsSuccess || pagesResult.Data == null)
            {
                throw new Exception($"Lỗi khi crawl pages: {pagesResult.ErrorMessage}");
            }

            var pageUrls = pagesResult.Data.ToList();
            var savedCount = await _mangaService.SavePagesAsync(chapterId, pageUrls, cancellationToken);

            job.Status = CrawlJobStatus.Completed;
            job.SuccessItems = savedCount;
            job.ProcessedItems = pageUrls.Count;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            return CrawlerResult<int>.Success(savedCount);
        }
        catch (Exception ex)
        {
            job.Status = CrawlJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.StackTrace = ex.StackTrace;
            job.UpdatedAt = DateTime.UtcNow;
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                _unitOfWork.CrawlJobs.Update(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            return CrawlerResult<int>.Failure($"Lỗi khi crawl pages: {ex.Message}", ex);
        }
    }

    #region Private Methods

    /// <summary>
    /// Crawl manga đầy đủ (internal method)
    /// </summary>
    private async Task<CrawlerResult<Guid>> CrawlMangaFullInternalAsync(
        Source source,
        string mangaUrl,
        CrawlJob job,
        bool skipExisting,
        IMangaCrawler mangaCrawler,
        IChapterCrawler chapterCrawler,
        IPageCrawler pageCrawler,
        CancellationToken cancellationToken)
    {
        var context = CreateCrawlerContext(source, job, cancellationToken);

        // Bước 1: Crawl manga detail
        var mangaResult = await mangaCrawler.CrawlMangaAsync(mangaUrl, context);
        if (!mangaResult.IsSuccess || mangaResult.Data == null)
        {
            return CrawlerResult<Guid>.Failure($"Lỗi khi crawl manga: {mangaResult.ErrorMessage}", mangaResult.Exception);
        }

        var mangaData = mangaResult.Data;

        // Kiểm tra nếu đã tồn tại và skip
        if (skipExisting && !string.IsNullOrEmpty(mangaData.SourceMangaId))
        {
            var exists = await _mangaService.MangaExistsAsync(source.Id, mangaData.SourceMangaId, cancellationToken);
            if (exists)
            {
                LogJob(job, $"Manga {mangaData.Title} đã tồn tại, bỏ qua", LogLevel.Info, cancellationToken);
                // Sử dụng MangaService thay vì gọi trực tiếp từ _unitOfWork để đảm bảo thread-safety
                // Note: Cần thêm method GetMangaBySourceIdAsync vào IMangaService nếu cần
                // Tạm thời bỏ qua việc lấy existingManga.Id vì đã có exists check
                await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
                try
                {
                    var existingManga = await _unitOfWork.Mangas.FindBySourceIdAsync(source.Id, mangaData.SourceMangaId, cancellationToken);
                    if (existingManga != null)
                    {
                        return CrawlerResult<Guid>.Success(existingManga.Id);
                    }
                }
                finally
                {
                    SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
                }
            }
        }

        var manga = await _mangaService.SaveOrUpdateMangaAsync(mangaData, source.Id, cancellationToken);
        LogJob(job, $"Đã lưu manga: {manga.Title}", LogLevel.Info, cancellationToken);

        // Bước 2: Crawl chapters với parallel processing
        // Nếu mangaData đã có chapters từ lần crawl trước, sử dụng luôn
        var chapters = mangaData.Chapters?.ToList() ?? new List<ChapterCrawlData>();
        
        // Nếu không có chapters, thử crawl riêng
        if (chapters.Count == 0)
        {
            var chaptersResult = await chapterCrawler.CrawlChaptersAsync(mangaUrl, context);
            if (!chaptersResult.IsSuccess || chaptersResult.Data == null)
            {
                LogJob(job, $"Cảnh báo: Không thể crawl chapters: {chaptersResult.ErrorMessage}", LogLevel.Warning, cancellationToken);
                return CrawlerResult<Guid>.Success(manga.Id); // Vẫn trả về success vì đã lưu được manga
            }
            chapters = chaptersResult.Data.ToList();
        }
        
        LogJob(job, $"Tìm thấy {chapters.Count} chapters", LogLevel.Info, cancellationToken);

        // Bước 3: Multi-task crawl chapters và pages song song
        // Strategy: Crawl tất cả chapters và pages song song, sau đó save song song vào database
        const int maxChapterCrawlConcurrency = 20; // Tăng số chapters crawl đồng thời
        const int maxChapterSaveConcurrency = 10; // Số chapters save đồng thời vào database
        var chapterCrawlSemaphore = new SemaphoreSlim(maxChapterCrawlConcurrency, maxChapterCrawlConcurrency);
        var chapterTasks = new List<Task<(ChapterCrawlData chapterData, List<string>? pageUrls)>>();

        // Lọc chapters cần crawl (skip existing nếu cần) - batch check để tối ưu
        var chaptersToCrawl = new List<ChapterCrawlData>();
        
        if (skipExisting)
        {
            // Batch check: Lấy tất cả sourceChapterIds đã tồn tại trong một query duy nhất
            var sourceChapterIds = chapters
                .Where(c => !string.IsNullOrEmpty(c.SourceChapterId))
                .Select(c => c.SourceChapterId!)
                .ToList();

            if (sourceChapterIds.Count > 0)
            {
                var existingChapterIds = await _mangaService.GetExistingChapterIdsAsync(
                    manga.Id, 
                    sourceChapterIds, 
                    cancellationToken);

                // Lọc chapters chưa tồn tại
                chaptersToCrawl = chapters
                    .Where(c => string.IsNullOrEmpty(c.SourceChapterId) || 
                               !existingChapterIds.Contains(c.SourceChapterId))
                    .ToList();
            }
            else
            {
                chaptersToCrawl = chapters;
            }
        }
        else
        {
            chaptersToCrawl = chapters;
        }

        LogJob(job, $"Sẽ crawl {chaptersToCrawl.Count} chapters mới (bỏ qua {chapters.Count - chaptersToCrawl.Count} chapters đã tồn tại)", LogLevel.Info, cancellationToken);

        // Multi-task crawl tất cả page URLs song song (không touch database)
        foreach (var chapterData in chaptersToCrawl)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var chapter = chapterData; // Capture for closure
            var task = Task.Run(async () =>
            {
                await chapterCrawlSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Crawl pages cho chapter này (multi-task với các chapters khác)
                    var pagesResult = await pageCrawler.CrawlPageUrlsAsync(chapter.SourceUrl, context);
                    if (pagesResult.IsSuccess && pagesResult.Data != null)
                    {
                        var pageUrls = pagesResult.Data.ToList();
                        LogJob(job, $"Đã crawl {pageUrls.Count} pages cho chapter {chapter.Title}", LogLevel.Debug, cancellationToken);
                        return (chapterData: chapter, pageUrls: pageUrls);
                    }
                    return (chapterData: chapter, pageUrls: (List<string>?)null);
                }
                catch (Exception ex)
                {
                    LogJob(job, $"Lỗi khi crawl pages cho chapter {chapter.Title}: {ex.Message}", LogLevel.Error, cancellationToken, ex);
                    return (chapterData: chapter, pageUrls: (List<string>?)null);
                }
                finally
                {
                    chapterCrawlSemaphore.Release();
                }
            }, cancellationToken);

            chapterTasks.Add(task);
        }

        // Đợi tất cả chapters và pages crawl hoàn thành (chạy song song)
        if (chapterTasks.Count > 0)
        {
            LogJob(job, $"Đang crawl {chapterTasks.Count} chapters và pages song song...", LogLevel.Info, cancellationToken);
            var crawlResults = await Task.WhenAll(chapterTasks);
            LogJob(job, $"Hoàn thành crawl {crawlResults.Length} chapters", LogLevel.Info, cancellationToken);

            // Bước 4: Save chapters và pages song song (không cần thứ tự giữa chapters, chỉ cần thứ tự trong từng chapter)
            var savedChapters = 0;
            var savedPages = 0;
            var saveLockObject = new object();
            
            // Sử dụng semaphore để control database writes nhưng vẫn parallel
            var saveSemaphore = new SemaphoreSlim(maxChapterSaveConcurrency, maxChapterSaveConcurrency);
            var saveTasks = new List<Task>();
            
            foreach (var result in crawlResults)
            {
                var chapterData = result.chapterData;
                var pageUrls = result.pageUrls;
                
                if (cancellationToken.IsCancellationRequested)
                    break;

                var saveTask = Task.Run(async () =>
                {
                    await saveSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        // Lưu chapter
                        var savedChapter = await _mangaService.SaveOrUpdateChapterAsync(chapterData, manga.Id, cancellationToken);
                        
                        int pageCount = 0;
                        // Lưu pages nếu có (thứ tự trong chapter đã được đảm bảo bởi SavePagesAsync)
                        if (pageUrls != null && pageUrls.Any())
                        {
                            pageCount = await _mangaService.SavePagesAsync(savedChapter.Id, pageUrls, cancellationToken);
                        }
                        
                        lock (saveLockObject)
                        {
                            savedChapters++;
                            savedPages += pageCount;
                        }
                        
                        LogJob(job, $"Đã lưu chapter {savedChapter.Title} với {pageCount} pages", LogLevel.Debug, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogJob(job, $"Lỗi khi lưu chapter {chapterData.Title}: {ex.Message}", LogLevel.Error, cancellationToken, ex);
                    }
                    finally
                    {
                        saveSemaphore.Release();
                    }
                }, cancellationToken);
                
                saveTasks.Add(saveTask);
            }
            
            // Đợi tất cả save tasks hoàn thành
            await Task.WhenAll(saveTasks);
            
            LogJob(job, $"Đã lưu {savedChapters} chapters và {savedPages} pages vào database", LogLevel.Info, cancellationToken);
        }
        else
        {
            LogJob(job, "Không có chapters mới cần crawl", LogLevel.Info, cancellationToken);
        }

        LogJob(job, $"Hoàn thành crawl manga {manga.Title} với {chapters.Count} chapters", LogLevel.Info, cancellationToken);

        return CrawlerResult<Guid>.Success(manga.Id);
    }

    /// <summary>
    /// Tạo CrawlJob mới
    /// </summary>
    private async Task<CrawlJob> CreateCrawlJobAsync(
        Source source,
        CrawlJobType type,
        string name,
        string? startUrl,
        int? maxPages,
        CancellationToken cancellationToken)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            // Đảm bảo Source được track trong context trước khi tạo CrawlJob
            var trackedSource = await _context.Sources.FindAsync(new object[] { source.Id }, cancellationToken);
            if (trackedSource == null)
            {
                // Nếu Source chưa có trong context, attach nó với state Unchanged
                _context.Sources.Attach(source);
                _context.Entry(source).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                trackedSource = source;
            }
            
            var job = new CrawlJob
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = type,
                Status = CrawlJobStatus.Pending,
                SourceId = source.Id,
                StartUrl = startUrl,
                MaxPages = maxPages,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Set navigation property để EF Core không cần detect changes
            job.Source = trackedSource;

            await _unitOfWork.CrawlJobs.AddAsync(job, cancellationToken);
            
            // Tắt change detection cho Source để tránh null reference
            _context.Entry(trackedSource).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return job;
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    /// <summary>
    /// Cập nhật status của job
    /// </summary>
    private async Task UpdateJobStatusAsync(CrawlJob job, CrawlJobStatus status, CancellationToken cancellationToken)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            job.Status = status;
            if (status == CrawlJobStatus.Running && job.StartedAt == null)
            {
                job.StartedAt = DateTime.UtcNow;
            }
            job.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CrawlJobs.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    /// <summary>
    /// Cập nhật progress của job
    /// </summary>
    private async Task UpdateJobProgressAsync(
        CrawlJob job,
        int processedItems,
        int successItems,
        int failedItems,
        CancellationToken cancellationToken)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            job.ProcessedItems = processedItems;
            job.SuccessItems = successItems;
            job.FailedItems = failedItems;
            job.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CrawlJobs.Update(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    /// <summary>
    /// Tạo CrawlerContext
    /// </summary>
    private CrawlerContext CreateCrawlerContext(Source source, CrawlJob job, CancellationToken cancellationToken)
    {
        return new CrawlerContext
        {
            Source = source,
            CrawlJob = job,
            StartUrl = job.StartUrl ?? source.BaseUrl,
            CancellationToken = cancellationToken,
            OnLog = (message, level) => LogJob(job, message, level, cancellationToken),
            OnProgress = (processed, total) => UpdateJobProgressAsync(job, processed, job.SuccessItems, job.FailedItems, cancellationToken).Wait(cancellationToken)
        };
    }

    /// <summary>
    /// Log vào CrawlJobLog
    /// </summary>
    private void LogJob(
        CrawlJob job,
        string message,
        Application.Common.Models.LogLevel level,
        CancellationToken cancellationToken,
        Exception? exception = null)
    {
        // Fire and forget - không block thread
        _ = Task.Run(async () =>
        {
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
            try
            {
                var log = new CrawlJobLog
                {
                    Id = Guid.NewGuid(),
                    CrawlJobId = job.Id,
                    Message = message,
                    Level = ConvertLogLevel(level),
                    Exception = exception?.Message,
                    StackTrace = exception?.StackTrace,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CrawlJobLogs.Add(log);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Ignore log errors
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Convert LogLevel từ Application.Common.Models sang Domain.Entities
    /// </summary>
    private Domain.Entities.LogLevel ConvertLogLevel(Application.Common.Models.LogLevel level)
    {
        return level switch
        {
            Application.Common.Models.LogLevel.Debug => Domain.Entities.LogLevel.Debug,
            Application.Common.Models.LogLevel.Info => Domain.Entities.LogLevel.Info,
            Application.Common.Models.LogLevel.Warning => Domain.Entities.LogLevel.Warning,
            Application.Common.Models.LogLevel.Error => Domain.Entities.LogLevel.Error,
            Application.Common.Models.LogLevel.Critical => Domain.Entities.LogLevel.Critical,
            _ => Domain.Entities.LogLevel.Info
        };
    }

    #endregion
}

