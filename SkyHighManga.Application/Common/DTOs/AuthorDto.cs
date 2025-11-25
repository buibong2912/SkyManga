namespace SkyHighManga.Application.Common.DTOs;

public class AuthorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AlternativeName { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public int MangaCount { get; set; }
}

