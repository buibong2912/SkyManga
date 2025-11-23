using SkyHighManga.Application.Common.Models;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Crawlers;

/// <summary>
/// Interface cơ bản cho tất cả crawlers
/// </summary>
public interface ICrawler
{
    /// <summary>
    /// Tên crawler (ví dụ: "NettruyenCrawler")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Source type mà crawler này hỗ trợ
    /// </summary>
    SourceType SupportedSourceType { get; }

    /// <summary>
    /// Kiểm tra crawler có thể crawl source này không
    /// </summary>
    bool CanCrawl(Source source);

    /// <summary>
    /// Test connection đến source
    /// </summary>
    Task<bool> TestConnectionAsync(Source source, CancellationToken cancellationToken = default);
}

