using MediatR;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Common.Responses;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Features.Manga.Queries;

public class GetMangasQuery : IRequest<PagedResponse<MangaDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public MangaStatus? Status { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid? GenreId { get; set; }
    public string? SortBy { get; set; } = "UpdatedAt";
    public bool SortDescending { get; set; } = true;
}

