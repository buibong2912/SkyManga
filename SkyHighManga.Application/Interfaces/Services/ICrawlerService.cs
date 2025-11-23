using SkyHighManga.Application.Common.Models;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Services;

/// <summary>
/// Service để quản lý và thực thi crawl jobs
/// </summary>
public interface ICrawlerService
{
    /// <summary>
    /// Crawl toàn bộ source
    /// </summary>
    Task<CrawlerResult<int>> CrawlSourceAsync(
        Source source,
        CrawlJob? crawlJob = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crawl một manga cụ thể
    /// </summary>
    Task<CrawlerResult<MangaCrawlData>> CrawlMangaAsync(
        Source source,
        string mangaUrl,
        CrawlJob? crawlJob = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update manga đã có (chỉ crawl chapters mới)
    /// </summary>
    Task<CrawlerResult<int>> UpdateMangaAsync(
        Source source,
        Guid mangaId,
        CrawlJob? crawlJob = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm và crawl manga
    /// </summary>
    Task<CrawlerListResult<MangaCrawlData>> SearchAndCrawlAsync(
        Source source,
        string keyword,
        CrawlJob? crawlJob = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crawl chapters của một manga
    /// </summary>
    Task<CrawlerListResult<ChapterCrawlData>> CrawlChaptersAsync(
        Source source,
        string mangaUrl,
        CrawlJob? crawlJob = null,
        int? maxChapters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download pages của một chapter
    /// </summary>
    Task<CrawlerListResult<byte[]>> DownloadChapterPagesAsync(
        Source source,
        string chapterUrl,
        CrawlJob? crawlJob = null,
        CancellationToken cancellationToken = default);
}

