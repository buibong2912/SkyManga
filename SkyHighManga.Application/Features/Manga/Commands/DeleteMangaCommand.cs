using MediatR;

namespace SkyHighManga.Application.Features.Manga.Commands;

public class DeleteMangaCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

