using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Chapter.Queries;

public class GetChapterByIdQuery : IRequest<ChapterDto?>
{
    public Guid Id { get; set; }
}

