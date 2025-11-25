using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Contracts;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Consumers;

/// <summary>
/// Consumer để crawl manga details và chapters
/// Sau đó publish messages cho từng chapter để crawl pages
/// </summary>
public class CrawlMangaConsumer : IConsumer<CrawlMangaCommand>
{
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly IMangaService _mangaService;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CrawlMangaConsumer> _logger;

    public CrawlMangaConsumer(
        ICrawlerFactory crawlerFactory,
        IMangaService mangaService,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CrawlMangaConsumer> logger)
    {
        _crawlerFactory = crawlerFactory;
        _mangaService = mangaService;
        _context = context;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CrawlMangaCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Bắt đầu crawl manga: {MangaTitle} ({MangaUrl})", command.MangaTitle, command.MangaUrl);

        try
        {
            // Lấy source
            var source = await _context.Sources.FindAsync(new object[] { command.SourceId }, context.CancellationToken);
            if (source == null)
            {
                _logger.LogError("Không tìm thấy source với ID {SourceId}", command.SourceId);
                return;
            }

            // Lấy crawl job
            var crawlJob = await _unitOfWork.CrawlJobs.GetByIdAsync(command.CrawlJobId, context.CancellationToken);
            if (crawlJob == null)
            {
                _logger.LogError("Không tìm thấy crawl job với ID {CrawlJobId}", command.CrawlJobId);
                return;
            }

            // Tạo crawlers
            var mangaCrawler = _crawlerFactory.CreateMangaCrawler(source);
            var chapterCrawler = _crawlerFactory.CreateChapterCrawler(source);

            if (mangaCrawler == null || chapterCrawler == null)
            {
                _logger.LogError("Không thể tạo crawlers cho source {SourceId}", command.SourceId);
                return;
            }

            // Crawl manga details
            var crawlerContext = new SkyHighManga.Application.Common.Models.CrawlerContext
            {
                Source = source,
                CrawlJob = crawlJob,
                StartUrl = command.MangaUrl,
                CancellationToken = context.CancellationToken
            };

            var mangaResult = await mangaCrawler.CrawlMangaAsync(command.MangaUrl, crawlerContext);
            if (!mangaResult.IsSuccess || mangaResult.Data == null)
            {
                _logger.LogError("Lỗi khi crawl manga {MangaUrl}: {ErrorMessage}", command.MangaUrl, mangaResult.ErrorMessage);
                return;
            }

            var mangaData = mangaResult.Data;

            // Kiểm tra nếu đã tồn tại và skip
            if (command.SkipExisting && !string.IsNullOrEmpty(mangaData.SourceMangaId))
            {
                var exists = await _mangaService.MangaExistsAsync(source.Id, mangaData.SourceMangaId, context.CancellationToken);
                if (exists)
                {
                    _logger.LogInformation("Manga {MangaTitle} đã tồn tại, bỏ qua", mangaData.Title);
                    // Vẫn cần lấy manga ID để crawl chapters mới
                    await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(context.CancellationToken);
                    try
                    {
                        var existingManga = await _unitOfWork.Mangas.FindBySourceIdAsync(source.Id, mangaData.SourceMangaId, context.CancellationToken);
                        if (existingManga != null)
                        {
                            await PublishChapterCommands(existingManga.Id, mangaData.Chapters, command, context);
                        }
                    }
                    finally
                    {
                        SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
                    }
                    return;
                }
            }

            // Lưu manga
            var manga = await _mangaService.SaveOrUpdateMangaAsync(mangaData, source.Id, context.CancellationToken);
            _logger.LogInformation("Đã lưu manga: {MangaTitle}", manga.Title);

            // Lấy chapters (từ mangaData hoặc crawl riêng)
            var chapters = mangaData.Chapters?.ToList() ?? new List<SkyHighManga.Application.Common.Models.ChapterCrawlData>();
            
            if (chapters.Count == 0)
            {
                var chaptersResult = await chapterCrawler.CrawlChaptersAsync(command.MangaUrl, crawlerContext);
                if (chaptersResult.IsSuccess && chaptersResult.Data != null)
                {
                    chapters = chaptersResult.Data.ToList();
                }
            }

            _logger.LogInformation("Tìm thấy {Count} chapters cho manga {MangaTitle}", chapters.Count, manga.Title);

            // Publish messages cho từng chapter để crawl pages song song
            await PublishChapterCommands(manga.Id, chapters, command, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý CrawlMangaCommand cho {MangaUrl}", command.MangaUrl);
            throw;
        }
    }

    private async Task PublishChapterCommands(
        Guid mangaId,
        List<SkyHighManga.Application.Common.Models.ChapterCrawlData> chapters,
        CrawlMangaCommand command,
        ConsumeContext<CrawlMangaCommand> context)
    {
        // Lọc chapters cần crawl (skip existing nếu cần)
        var chaptersToCrawl = new List<SkyHighManga.Application.Common.Models.ChapterCrawlData>();
        
        if (command.SkipExisting)
        {
            // Batch check: Lấy tất cả sourceChapterIds đã tồn tại trong một query duy nhất
            var sourceChapterIds = chapters
                .Where(c => !string.IsNullOrEmpty(c.SourceChapterId))
                .Select(c => c.SourceChapterId!)
                .ToList();

            if (sourceChapterIds.Count > 0)
            {
                var existingChapterIds = await _mangaService.GetExistingChapterIdsAsync(
                    mangaId, 
                    sourceChapterIds, 
                    context.CancellationToken);

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

        if (chaptersToCrawl.Count == 0)
        {
            _logger.LogInformation("Tất cả chapters đã tồn tại, không cần crawl");
            return;
        }

        _logger.LogInformation("Sẽ crawl {Count} chapters mới (bỏ qua {Skipped} chapters đã tồn tại)", 
            chaptersToCrawl.Count, chapters.Count - chaptersToCrawl.Count);

        // Publish messages cho chapters cần crawl - publish tất cả cùng lúc để tối ưu tốc độ
        var publishTasks = chaptersToCrawl.Select(async chapter =>
        {
            await _publishEndpoint.Publish(new CrawlChapterCommand
            {
                SourceId = command.SourceId,
                CrawlJobId = command.CrawlJobId,
                MangaId = mangaId,
                ChapterUrl = chapter.SourceUrl,
                ChapterTitle = chapter.Title ?? "Unknown",
                SourceChapterId = chapter.SourceChapterId,
                SkipExisting = command.SkipExisting
            }, context.CancellationToken);
        });

        // Publish tất cả cùng lúc, không chờ từng batch
        await Task.WhenAll(publishTasks);

        _logger.LogInformation("✅ Đã publish {Count} messages để crawl chapters (đa luồng cao)", chaptersToCrawl.Count);
    }
}

