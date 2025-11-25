using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Common.DTOs;

public class MangaDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AlternativeTitle { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public MangaStatus Status { get; set; }
    public int? YearOfRelease { get; set; }
    public string? OriginalLanguage { get; set; }
    public int ViewCount { get; set; }
    public double? Rating { get; set; }
    public int RatingCount { get; set; }
    public Guid? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public Guid SourceId { get; set; }
    public string? SourceMangaId { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastCrawledAt { get; set; }
    public bool IsActive { get; set; }
    public List<GenreDto> Genres { get; set; } = new();
    public int ChapterCount { get; set; }
}

