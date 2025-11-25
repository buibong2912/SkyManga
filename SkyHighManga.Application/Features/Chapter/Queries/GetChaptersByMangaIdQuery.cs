using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Chapter.Queries;

public class GetChaptersByMangaIdQuery : IRequest<List<ChapterDto>>
{
    public Guid MangaId { get; set; }
}

