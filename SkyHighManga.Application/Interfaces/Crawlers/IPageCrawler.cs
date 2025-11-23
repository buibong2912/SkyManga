using SkyHighManga.Application.Common.Models;

namespace SkyHighManga.Application.Interfaces.Crawlers;

/// <summary>
/// Interface cho crawler crawl pages/images
/// </summary>
public interface IPageCrawler : ICrawler
{
    /// <summary>
    /// Crawl danh sách URLs của các pages trong chapter
    /// </summary>
    Task<CrawlerListResult<string>> CrawlPageUrlsAsync(
        string chapterUrl,
        CrawlerContext context);

    /// <summary>
    /// Download một page/image
    /// </summary>
    Task<CrawlerResult<byte[]>> DownloadPageAsync(
        string imageUrl,
        CrawlerContext context);

    /// <summary>
    /// Download nhiều pages
    /// </summary>
    Task<CrawlerListResult<byte[]>> DownloadPagesAsync(
        IEnumerable<string> imageUrls,
        CrawlerContext context,
        int? maxPages = null);
}

