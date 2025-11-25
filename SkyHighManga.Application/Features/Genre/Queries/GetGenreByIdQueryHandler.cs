using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Genre.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Genre.Queries;

public class GetGenreByIdQueryHandler : IRequestHandler<GetGenreByIdQuery, GenreDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetGenreByIdQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetGenreByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetGenreByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<GenreDto?> Handle(GetGenreByIdQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var genre = await _unitOfWork.Genres.GetByIdAsync(request.Id, cancellationToken);
            if (genre == null || !genre.IsActive)
                return null;

            return new GenreDto
            {
                Id = genre.Id,
                Name = genre.Name,
                Description = genre.Description,
                Slug = genre.Slug,
                CreatedAt = genre.CreatedAt,
                UpdatedAt = genre.UpdatedAt,
                IsActive = genre.IsActive,
                MangaCount = genre.MangaGenres.Count(mg => mg.Manga.IsActive)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

