using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Services;

public class MangaService : IMangaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public MangaService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Manga> SaveOrUpdateMangaAsync(
        MangaCrawlData crawlData,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrEmpty(crawlData.SourceMangaId))
            {
                throw new ArgumentException("SourceMangaId không được null hoặc empty", nameof(crawlData));
            }

            // Tìm manga đã tồn tại
            var existingManga = await _unitOfWork.Mangas.FindBySourceIdAsync(sourceId, crawlData.SourceMangaId, cancellationToken);

        if (existingManga != null)
        {
            // Cập nhật thông tin
            existingManga.Title = crawlData.Title;
            existingManga.Description = crawlData.Description;
            existingManga.CoverImageUrl = crawlData.CoverImageUrl;
            existingManga.Status = ParseMangaStatus(crawlData.Status);
            existingManga.Rating = crawlData.Rating;
            existingManga.ViewCount = crawlData.ViewCount;
            existingManga.SourceUrl = crawlData.SourceUrl;
            existingManga.UpdatedAt = DateTime.UtcNow;

            // Cập nhật author
            if (!string.IsNullOrEmpty(crawlData.AuthorName))
            {
                var author = await _unitOfWork.Authors.FindByNameAsync(crawlData.AuthorName, cancellationToken);
                if (author == null)
                {
                    author = new Author
                    {
                        Id = Guid.NewGuid(),
                        Name = crawlData.AuthorName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Authors.AddAsync(author, cancellationToken);
                }
                existingManga.AuthorId = author.Id;
            }

            _unitOfWork.Mangas.Update(existingManga);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return existingManga;
        }

        // Tạo mới manga
        var manga = new Manga
        {
            Id = Guid.NewGuid(),
            Title = crawlData.Title,
            Description = crawlData.Description,
            CoverImageUrl = crawlData.CoverImageUrl,
            Status = ParseMangaStatus(crawlData.Status),
            Rating = crawlData.Rating,
            ViewCount = crawlData.ViewCount,
            SourceId = sourceId,
            SourceMangaId = crawlData.SourceMangaId,
            SourceUrl = crawlData.SourceUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Tìm hoặc tạo author
        if (!string.IsNullOrEmpty(crawlData.AuthorName))
        {
            var author = await _unitOfWork.Authors.FindByNameAsync(crawlData.AuthorName, cancellationToken);
            if (author == null)
            {
                author = new Author
                {
                    Id = Guid.NewGuid(),
                    Name = crawlData.AuthorName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Authors.AddAsync(author, cancellationToken);
            }
            manga.AuthorId = author.Id;
        }

        await _unitOfWork.Mangas.AddAsync(manga, cancellationToken);
        
        // Ensure Source is tracked by context to avoid null reference
        var source = await _context.Sources.FindAsync(new object[] { sourceId }, cancellationToken);
        if (source != null)
        {
            _context.Entry(source).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Lưu genres sau khi manga đã được lưu
        if (crawlData.Genres != null && crawlData.Genres.Any())
        {
            // Xóa genres cũ nếu có
            var existingGenres = await _context.MangaGenres
                .Where(mg => mg.MangaId == manga.Id)
                .ToListAsync(cancellationToken);
            _context.MangaGenres.RemoveRange(existingGenres);

            // Thêm genres mới
            foreach (var genreName in crawlData.Genres)
            {
                var genre = await _unitOfWork.Genres.FindOrCreateAsync(genreName, cancellationToken);
                var mangaGenre = new MangaGenre
                {
                    Id = Guid.NewGuid(),
                    MangaId = manga.Id,
                    GenreId = genre.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MangaGenres.Add(mangaGenre);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        return manga;
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    public async Task<Chapter> SaveOrUpdateChapterAsync(
        ChapterCrawlData crawlData,
        Guid mangaId,
        CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            // Generate SourceChapterId nếu chưa có
            if (string.IsNullOrEmpty(crawlData.SourceChapterId))
        {
            if (!string.IsNullOrEmpty(crawlData.ChapterNumber))
            {
                crawlData.SourceChapterId = crawlData.ChapterNumber;
            }
            else if (crawlData.ChapterIndex.HasValue)
            {
                crawlData.SourceChapterId = crawlData.ChapterIndex.Value.ToString();
            }
            else if (!string.IsNullOrEmpty(crawlData.SourceUrl))
            {
                // Extract từ URL
                var urlParts = crawlData.SourceUrl.Split('/');
                crawlData.SourceChapterId = urlParts.LastOrDefault() ?? crawlData.SourceUrl;
            }
            else
            {
                throw new ArgumentException("SourceChapterId không được null hoặc empty và không thể generate từ dữ liệu có sẵn", nameof(crawlData));
            }
        }

        var existingChapter = await _unitOfWork.Chapters.FindBySourceIdAsync(mangaId, crawlData.SourceChapterId, cancellationToken);

        if (existingChapter != null)
        {
            // Cập nhật thông tin
            existingChapter.Title = crawlData.Title;
            existingChapter.ChapterNumber = crawlData.ChapterNumber;
            existingChapter.ChapterIndex = crawlData.ChapterIndex;
            existingChapter.SourceUrl = crawlData.SourceUrl;
            if (crawlData.PublishedAt.HasValue)
            {
                existingChapter.PublishedAt = crawlData.PublishedAt.Value;
            }
            existingChapter.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Chapters.Update(existingChapter);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return existingChapter;
        }

        // Tạo mới chapter
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            Title = crawlData.Title,
            ChapterNumber = crawlData.ChapterNumber,
            ChapterIndex = crawlData.ChapterIndex,
            MangaId = mangaId,
            SourceChapterId = crawlData.SourceChapterId,
            SourceUrl = crawlData.SourceUrl,
            PublishedAt = crawlData.PublishedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Chapters.AddAsync(chapter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return chapter;
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    public async Task<int> SavePagesAsync(
        Guid chapterId,
        IEnumerable<string> pageUrls,
        CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            var pageList = pageUrls.ToList();
            if (pageList.Count == 0)
            {
                return 0;
            }

            // Tối ưu: Load tất cả existing pages một lần thay vì query từng page
            var existingPages = (await _unitOfWork.Pages.GetByChapterIdAsync(chapterId, cancellationToken))
                .ToList();

            // Tạo dictionary để lookup nhanh theo PageNumber và SourcePageId
            var pagesByPageNumber = existingPages
                .Where(p => p.IsActive)
                .ToDictionary(p => p.PageNumber, p => p);
            
            var pagesBySourceId = existingPages
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.SourcePageId))
                .GroupBy(p => p.SourcePageId)
                .ToDictionary(g => g.Key!, g => g.First());

            var pagesToAdd = new List<Page>();
            var pagesToUpdate = new List<Page>();
            var savedCount = 0;
            var updatedCount = 0;

            for (int i = 0; i < pageList.Count; i++)
            {
                var pageUrl = pageList[i];
                var pageNumber = i + 1;
                var sourcePageId = ExtractPageIdFromUrl(pageUrl);

                // Kiểm tra page đã tồn tại theo PageNumber (lookup từ dictionary)
                if (pagesByPageNumber.TryGetValue(pageNumber, out var existingPage))
                {
                    // Update page nếu đã tồn tại (có thể URL đã thay đổi)
                    if (existingPage.ImageUrl != pageUrl || existingPage.SourcePageId != sourcePageId)
                    {
                        existingPage.ImageUrl = pageUrl;
                        existingPage.SourcePageId = sourcePageId;
                        existingPage.UpdatedAt = DateTime.UtcNow;
                        pagesToUpdate.Add(existingPage);
                        updatedCount++;
                    }
                    // Nếu không có thay đổi, skip
                    continue;
                }

                // Kiểm tra thêm bằng SourcePageId để tránh duplicate (lookup từ dictionary)
                if (!string.IsNullOrEmpty(sourcePageId) && pagesBySourceId.ContainsKey(sourcePageId))
                {
                    continue;
                }

                // Tạo page mới (sẽ batch add sau)
                var page = new Page
                {
                    Id = Guid.NewGuid(),
                    PageNumber = pageNumber,
                    ImageUrl = pageUrl,
                    ChapterId = chapterId,
                    SourcePageId = sourcePageId,
                    IsDownloaded = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                pagesToAdd.Add(page);
                savedCount++;
            }

            // Batch insert tất cả pages mới
            if (pagesToAdd.Count > 0)
            {
                await _unitOfWork.Pages.AddRangeAsync(pagesToAdd, cancellationToken);
            }

            // Batch update tất cả pages cần update
            if (pagesToUpdate.Count > 0)
            {
                _unitOfWork.Pages.UpdateRange(pagesToUpdate);
            }

            // Chỉ save một lần cho tất cả changes
            if (pagesToAdd.Count > 0 || pagesToUpdate.Count > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return savedCount + updatedCount;
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    public async Task<bool> MangaExistsAsync(Guid sourceId, string sourceMangaId, CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            return await _unitOfWork.Mangas.ExistsBySourceIdAsync(sourceId, sourceMangaId, cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    public async Task<bool> ChapterExistsAsync(Guid mangaId, string sourceChapterId, CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            return await _unitOfWork.Chapters.ExistsBySourceIdAsync(mangaId, sourceChapterId, cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    public async Task<HashSet<string>> GetExistingChapterIdsAsync(Guid mangaId, IEnumerable<string> sourceChapterIds, CancellationToken cancellationToken = default)
    {
        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            return await _unitOfWork.Chapters.GetExistingSourceChapterIdsAsync(mangaId, sourceChapterIds, cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }
    }

    private string ExtractPageIdFromUrl(string url)
    {
        // Extract page ID from URL (có thể là filename hoặc query parameter)
        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            if (segments.Length > 0)
            {
                return segments[segments.Length - 1];
            }
            return url;
        }
        catch
        {
            return url;
        }
    }

    private MangaStatus ParseMangaStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return MangaStatus.Unknown;

        var statusLower = status.ToLowerInvariant();
        
        if (statusLower.Contains("đang ra") || statusLower.Contains("ongoing") || statusLower.Contains("đang cập nhật"))
            return MangaStatus.Ongoing;
        
        if (statusLower.Contains("hoàn thành") || statusLower.Contains("completed") || statusLower.Contains("finished"))
            return MangaStatus.Completed;
        
        if (statusLower.Contains("tạm dừng") || statusLower.Contains("hiatus") || statusLower.Contains("paused"))
            return MangaStatus.OnHold;
        
        if (statusLower.Contains("hủy") || statusLower.Contains("cancelled") || statusLower.Contains("dropped"))
            return MangaStatus.Cancelled;

        return MangaStatus.Unknown;
    }
}

