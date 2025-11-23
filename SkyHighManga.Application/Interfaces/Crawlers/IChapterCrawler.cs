using SkyHighManga.Application.Common.Models;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Crawlers;

/// <summary>
/// Interface cho crawler crawl chapters
/// </summary>
public interface IChapterCrawler : ICrawler
{
    /// <summary>
    /// Crawl danh sách chapters của một manga
    /// </summary>
    Task<CrawlerListResult<ChapterCrawlData>> CrawlChaptersAsync(
        string mangaUrl,
        CrawlerContext context,
        int? maxChapters = null);

    /// <summary>
    /// Crawl một chapter cụ thể
    /// </summary>
    Task<CrawlerResult<ChapterCrawlData>> CrawlChapterAsync(
        string chapterUrl,
        CrawlerContext context);

    /// <summary>
    /// Crawl các chapters mới (chưa có trong database)
    /// </summary>
    Task<CrawlerListResult<ChapterCrawlData>> CrawlNewChaptersAsync(
        string mangaUrl,
        IEnumerable<string> existingChapterIds,
        CrawlerContext context);
}

