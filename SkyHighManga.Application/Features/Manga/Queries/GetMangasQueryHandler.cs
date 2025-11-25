using System.Threading;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Common.Responses;
using SkyHighManga.Application.Features.Manga.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Manga.Queries;

public class GetMangasQueryHandler : IRequestHandler<GetMangasQuery, PagedResponse<MangaDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMangasQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetMangasQueryHandler(IUnitOfWork unitOfWork, ILogger<GetMangasQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<PagedResponse<MangaDto>> Handle(GetMangasQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var query = _unitOfWork.Mangas.GetAll()
                .Include(m => m.Author)
                .Include(m => m.MangaGenres)
                    .ThenInclude(mg => mg.Genre)
                .Where(m => m.IsActive);

            // Search by title
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(m => 
                    m.Title.Contains(request.SearchTerm) ||
                    (m.AlternativeTitle != null && m.AlternativeTitle.Contains(request.SearchTerm)));
            }

            // Filter by status
            if (request.Status.HasValue)
            {
                query = query.Where(m => m.Status == request.Status.Value);
            }

            // Filter by author
            if (request.AuthorId.HasValue)
            {
                query = query.Where(m => m.AuthorId == request.AuthorId.Value);
            }

            // Filter by genre
            if (request.GenreId.HasValue)
            {
                query = query.Where(m => m.MangaGenres.Any(mg => mg.GenreId == request.GenreId.Value && mg.Genre.IsActive));
            }

            // Sorting
            query = request.SortBy?.ToLower() switch
            {
                "title" => request.SortDescending 
                    ? query.OrderByDescending(m => m.Title)
                    : query.OrderBy(m => m.Title),
                "viewcount" => request.SortDescending
                    ? query.OrderByDescending(m => m.ViewCount)
                    : query.OrderBy(m => m.ViewCount),
                "rating" => request.SortDescending
                    ? query.OrderByDescending(m => m.Rating ?? 0)
                    : query.OrderBy(m => m.Rating ?? 0),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(m => m.CreatedAt)
                    : query.OrderBy(m => m.CreatedAt),
                _ => request.SortDescending
                    ? query.OrderByDescending(m => m.UpdatedAt)
                    : query.OrderBy(m => m.UpdatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var mangas = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mangaDtos = mangas.Select(m => new MangaDto
            {
                Id = m.Id,
                Title = m.Title,
                AlternativeTitle = m.AlternativeTitle,
                Description = m.Description,
                CoverImageUrl = m.CoverImageUrl,
                ThumbnailUrl = m.ThumbnailUrl,
                Status = m.Status,
                YearOfRelease = m.YearOfRelease,
                OriginalLanguage = m.OriginalLanguage,
                ViewCount = m.ViewCount,
                Rating = m.Rating,
                RatingCount = m.RatingCount,
                AuthorId = m.AuthorId,
                AuthorName = m.Author?.Name,
                SourceId = m.SourceId,
                SourceMangaId = m.SourceMangaId,
                SourceUrl = m.SourceUrl,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                LastCrawledAt = m.LastCrawledAt,
                IsActive = m.IsActive,
                Genres = m.MangaGenres
                    .Where(mg => mg.Genre.IsActive)
                    .Select(mg => new GenreDto
                    {
                        Id = mg.Genre.Id,
                        Name = mg.Genre.Name,
                        Description = mg.Genre.Description,
                        Slug = mg.Genre.Slug,
                        CreatedAt = mg.Genre.CreatedAt,
                        UpdatedAt = mg.Genre.UpdatedAt,
                        IsActive = mg.Genre.IsActive
                    })
                    .ToList(),
                ChapterCount = m.Chapters.Count(c => c.IsActive)
            }).ToList();

            return new PagedResponse<MangaDto>
            {
                Items = mangaDtos,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

