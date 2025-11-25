using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Manga.Commands;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Manga.Commands;

public class UpdateMangaCommandHandler : IRequestHandler<UpdateMangaCommand, MangaDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateMangaCommandHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public UpdateMangaCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateMangaCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<MangaDto> Handle(UpdateMangaCommand request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var manga = await _unitOfWork.Mangas.GetFullAsync(request.Id, cancellationToken);
            if (manga == null || !manga.IsActive)
                throw new KeyNotFoundException($"Manga with ID {request.Id} not found");

            manga.Title = request.Title;
            manga.AlternativeTitle = request.AlternativeTitle;
            manga.Description = request.Description;
            manga.CoverImageUrl = request.CoverImageUrl;
            manga.ThumbnailUrl = request.ThumbnailUrl;
            manga.Status = request.Status;
            manga.YearOfRelease = request.YearOfRelease;
            manga.OriginalLanguage = request.OriginalLanguage;
            manga.AuthorId = request.AuthorId;
            manga.UpdatedAt = DateTime.UtcNow;

            // Update genres
            manga.MangaGenres.Clear();
            if (request.GenreIds.Any())
            {
                foreach (var genreId in request.GenreIds)
                {
                    var genre = await _unitOfWork.Genres.GetByIdAsync(genreId, cancellationToken);
                    if (genre != null && genre.IsActive)
                    {
                        manga.MangaGenres.Add(new Domain.Entities.MangaGenre
                        {
                            Id = Guid.NewGuid(),
                            MangaId = manga.Id,
                            GenreId = genreId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _unitOfWork.Mangas.Update(manga);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload
            var updatedManga = await _unitOfWork.Mangas.GetFullAsync(manga.Id, cancellationToken);
            if (updatedManga == null)
                throw new InvalidOperationException("Failed to update manga");

            return CreateMangaCommandHandler.MapToDto(updatedManga);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

