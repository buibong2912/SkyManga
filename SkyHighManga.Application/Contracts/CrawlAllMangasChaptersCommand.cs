namespace SkyHighManga.Application.Contracts;

/// <summary>
/// Command để crawl chapters cho tất cả mangas đã có trong database
/// Sử dụng khi đã có list manga, muốn crawl chapters và pages với đa luồng cao
/// </summary>
public record CrawlAllMangasChaptersCommand
{
    public Guid SourceId { get; init; }
    public Guid CrawlJobId { get; init; }
    public int? MaxMangas { get; init; } // Giới hạn số mangas để crawl (null = tất cả)
    public bool SkipExisting { get; init; } = true;
}

