using SkyHighManga.Application.Common.Models;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Services;

/// <summary>
/// Service để quản lý và thực thi crawl jobs với rate limiting và parallel processing
/// </summary>
public interface ICrawlJobService
{
    /// <summary>
    /// Crawl toàn bộ mangas từ search (14k+ items) với parallel processing
    /// </summary>
    Task<CrawlerResult<int>> CrawlAllMangasAsync(
        Source source,
        CrawlJob? crawlJob = null,
        int? maxPages = null,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crawl một manga với chapters và pages
    /// </summary>
    Task<CrawlerResult<Guid>> CrawlMangaFullAsync(
        Source source,
        string mangaUrl,
        CrawlJob? crawlJob = null,
        bool skipExisting = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crawl chapters cho một manga
    /// </summary>
    Task<CrawlerResult<int>> CrawlMangaChaptersAsync(
        Source source,
        Guid mangaId,
        CrawlJob? crawlJob = null,
        bool skipExisting = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crawl pages cho một chapter
    /// </summary>
    Task<CrawlerResult<int>> CrawlChapterPagesAsync(
        Source source,
        Guid chapterId,
        CrawlJob? crawlJob = null,
        bool skipExisting = true,
        CancellationToken cancellationToken = default);
}


