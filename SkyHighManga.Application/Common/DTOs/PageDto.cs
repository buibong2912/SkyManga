namespace SkyHighManga.Application.Common.DTOs;

public class PageDto
{
    public Guid Id { get; set; }
    public int PageNumber { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? LocalFilePath { get; set; }
    public long? FileSize { get; set; }
    public string? ImageFormat { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public Guid ChapterId { get; set; }
    public string? SourcePageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDownloaded { get; set; }
    public bool IsActive { get; set; }
}

