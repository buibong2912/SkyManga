using MediatR;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Features.Manga.Commands;

public class CreateMangaCommand : IRequest<MangaDto>
{
    public string Title { get; set; } = string.Empty;
    public string? AlternativeTitle { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public MangaStatus Status { get; set; } = MangaStatus.Unknown;
    public int? YearOfRelease { get; set; }
    public string? OriginalLanguage { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid SourceId { get; set; }
    public string? SourceMangaId { get; set; }
    public string? SourceUrl { get; set; }
    public List<Guid> GenreIds { get; set; } = new();
}

