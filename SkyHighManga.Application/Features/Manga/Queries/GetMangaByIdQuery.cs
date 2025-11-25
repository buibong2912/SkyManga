using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Manga.Queries;

public class GetMangaByIdQuery : IRequest<MangaDto?>
{
    public Guid Id { get; set; }
}

