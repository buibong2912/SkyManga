namespace SkyHighManga.Application.Contracts;

/// <summary>
/// Command để crawl danh sách manga từ search pages
/// </summary>
public record CrawlMangaListCommand
{
    public Guid SourceId { get; init; }
    public Guid CrawlJobId { get; init; }
    public int? MaxPages { get; init; }
    public string BaseSearchUrl { get; init; } = string.Empty;
}

