namespace SkyHighManga.Application.Contracts;

/// <summary>
/// Command để crawl một manga cụ thể (details + chapters)
/// </summary>
public record CrawlMangaCommand
{
    public Guid SourceId { get; init; }
    public Guid CrawlJobId { get; init; }
    public string MangaUrl { get; init; } = string.Empty;
    public string MangaTitle { get; init; } = string.Empty;
    public bool SkipExisting { get; init; } = true;
}

