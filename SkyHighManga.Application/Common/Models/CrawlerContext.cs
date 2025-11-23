using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Common.Models;

/// <summary>
/// Context chứa thông tin cần thiết cho crawler
/// </summary>
public class CrawlerContext
{
    /// <summary>
    /// Source đang crawl
    /// </summary>
    public Source Source { get; set; } = null!;

    /// <summary>
    /// CrawlJob hiện tại (nếu có)
    /// </summary>
    public CrawlJob? CrawlJob { get; set; }

    /// <summary>
    /// URL bắt đầu crawl
    /// </summary>
    public string StartUrl { get; set; } = string.Empty;

    /// <summary>
    /// Configuration từ Source.ConfigurationJson
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Cancellation token để hủy crawl
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    /// <summary>
    /// Callback để log progress
    /// </summary>
    public Action<string, LogLevel>? OnLog { get; set; }

    /// <summary>
    /// Callback để update progress
    /// </summary>
    public Action<int, int>? OnProgress { get; set; }
}

/// <summary>
/// Mức độ log
/// </summary>
public enum LogLevel
{
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

