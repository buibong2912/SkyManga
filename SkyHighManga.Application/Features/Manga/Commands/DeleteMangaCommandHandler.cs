using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Features.Manga.Commands;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Manga.Commands;

public class DeleteMangaCommandHandler : IRequestHandler<DeleteMangaCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMangaCommandHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public DeleteMangaCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteMangaCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<bool> Handle(DeleteMangaCommand request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var manga = await _unitOfWork.Mangas.GetByIdAsync(request.Id, cancellationToken);
            if (manga == null)
                return false;

            // Soft delete
            manga.IsActive = false;
            manga.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Mangas.Update(manga);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

