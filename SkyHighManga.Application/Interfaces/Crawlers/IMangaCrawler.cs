using SkyHighManga.Application.Common.Models;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Crawlers;

/// <summary>
/// Interface cho crawler crawl manga
/// </summary>
public interface IMangaCrawler : ICrawler
{
    /// <summary>
    /// Crawl danh sách manga từ trang danh sách
    /// </summary>
    Task<CrawlerListResult<MangaCrawlData>> CrawlMangaListAsync(
        CrawlerContext context,
        int? maxItems = null);

    /// <summary>
    /// Crawl một manga cụ thể từ URL
    /// </summary>
    Task<CrawlerResult<MangaCrawlData>> CrawlMangaAsync(
        string mangaUrl,
        CrawlerContext context);

    /// <summary>
    /// Crawl thông tin chi tiết manga (không bao gồm chapters)
    /// </summary>
    Task<CrawlerResult<MangaCrawlData>> CrawlMangaDetailsAsync(
        string mangaUrl,
        CrawlerContext context);

    /// <summary>
    /// Tìm kiếm manga theo keyword
    /// </summary>
    /// <param name="keyword">Từ khóa tìm kiếm</param>
    /// <param name="context">Context cho crawler</param>
    /// <param name="maxResults">Số lượng kết quả tối đa (null = tất cả)</param>
    /// <param name="maxPages">Số trang tối đa để crawl (null = chỉ trang đầu tiên, 0 = tất cả các trang)</param>
    Task<CrawlerListResult<MangaCrawlData>> SearchMangaAsync(
        string keyword,
        CrawlerContext context,
        int? maxResults = null,
        int? maxPages = null);
}

