using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Page.Queries;

public class GetPagesByChapterIdQuery : IRequest<List<PageDto>>
{
    public Guid ChapterId { get; set; }
}

