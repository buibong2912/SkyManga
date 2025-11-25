namespace SkyHighManga.Application.Common.DTOs;

public class ChapterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ChapterNumber { get; set; }
    public int? ChapterIndex { get; set; }
    public int PageCount { get; set; }
    public Guid MangaId { get; set; }
    public string? MangaTitle { get; set; }
    public string? SourceChapterId { get; set; }
    public string? SourceUrl { get; set; }
    public int CountView { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsActive { get; set; }
}

