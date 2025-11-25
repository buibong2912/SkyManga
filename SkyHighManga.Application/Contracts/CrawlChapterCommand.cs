namespace SkyHighManga.Application.Contracts;

/// <summary>
/// Command để crawl một chapter cụ thể (pages)
/// </summary>
public record CrawlChapterCommand
{
    public Guid SourceId { get; init; }
    public Guid CrawlJobId { get; init; }
    public Guid MangaId { get; init; }
    public string ChapterUrl { get; init; } = string.Empty;
    public string ChapterTitle { get; init; } = string.Empty;
    public string? SourceChapterId { get; init; }
    public bool SkipExisting { get; init; } = true;
}

