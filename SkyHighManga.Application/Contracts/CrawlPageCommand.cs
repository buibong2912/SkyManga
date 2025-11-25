namespace SkyHighManga.Application.Contracts;

/// <summary>
/// Command để crawl pages cho một chapter
/// </summary>
public record CrawlPageCommand
{
    public Guid SourceId { get; init; }
    public Guid CrawlJobId { get; init; }
    public Guid ChapterId { get; init; }
    public string ChapterUrl { get; init; } = string.Empty;
    public bool SkipExisting { get; init; } = true;
}

