using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Crawlers;

/// <summary>
/// Factory để tạo crawler instances
/// </summary>
public interface ICrawlerFactory
{
    /// <summary>
    /// Tạo crawler từ Source
    /// </summary>
    ICrawler? CreateCrawler(Source source);

    /// <summary>
    /// Tạo crawler từ tên class
    /// </summary>
    ICrawler? CreateCrawler(string crawlerClassName);

    /// <summary>
    /// Tạo manga crawler từ Source
    /// </summary>
    IMangaCrawler? CreateMangaCrawler(Source source);

    /// <summary>
    /// Tạo chapter crawler từ Source
    /// </summary>
    IChapterCrawler? CreateChapterCrawler(Source source);

    /// <summary>
    /// Tạo page crawler từ Source
    /// </summary>
    IPageCrawler? CreatePageCrawler(Source source);

    /// <summary>
    /// Đăng ký crawler type
    /// </summary>
    void RegisterCrawler<T>(string name) where T : class, ICrawler;

    /// <summary>
    /// Lấy danh sách tất cả crawlers đã đăng ký
    /// </summary>
    IEnumerable<string> GetRegisteredCrawlers();
}

