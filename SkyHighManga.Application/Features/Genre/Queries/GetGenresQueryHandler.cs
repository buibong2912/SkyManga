using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Genre.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Genre.Queries;

public class GetGenresQueryHandler : IRequestHandler<GetGenresQuery, List<GenreDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetGenresQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetGenresQueryHandler(IUnitOfWork unitOfWork, ILogger<GetGenresQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<List<GenreDto>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var genres = await _unitOfWork.Genres.GetAllAsync(cancellationToken);
            
            var query = genres.Where(g => g.IsActive).AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(g => 
                    g.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            return query
                .OrderBy(g => g.Name)
                .Select(g => new GenreDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Slug = g.Slug,
                    CreatedAt = g.CreatedAt,
                    UpdatedAt = g.UpdatedAt,
                    IsActive = g.IsActive,
                    MangaCount = g.MangaGenres.Count(mg => mg.Manga.IsActive)
                })
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

