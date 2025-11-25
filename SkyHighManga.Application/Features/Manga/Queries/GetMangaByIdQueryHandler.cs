using System.Threading;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Manga.Queries;
using SkyHighManga.Application.Interfaces.Repositories;

namespace SkyHighManga.Application.Features.Manga.Queries;

public class GetMangaByIdQueryHandler : IRequestHandler<GetMangaByIdQuery, MangaDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMangaByIdQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetMangaByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetMangaByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<MangaDto?> Handle(GetMangaByIdQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var manga = await _unitOfWork.Mangas.GetFullAsync(request.Id, cancellationToken);
            if (manga == null || !manga.IsActive)
                return null;

            return new MangaDto
            {
                Id = manga.Id,
                Title = manga.Title,
                AlternativeTitle = manga.AlternativeTitle,
                Description = manga.Description,
                CoverImageUrl = manga.CoverImageUrl,
                ThumbnailUrl = manga.ThumbnailUrl,
                Status = manga.Status,
                YearOfRelease = manga.YearOfRelease,
                OriginalLanguage = manga.OriginalLanguage,
                ViewCount = manga.ViewCount,
                Rating = manga.Rating,
                RatingCount = manga.RatingCount,
                AuthorId = manga.AuthorId,
                AuthorName = manga.Author?.Name,
                SourceId = manga.SourceId,
                SourceMangaId = manga.SourceMangaId,
                SourceUrl = manga.SourceUrl,
                CreatedAt = manga.CreatedAt,
                UpdatedAt = manga.UpdatedAt,
                LastCrawledAt = manga.LastCrawledAt,
                IsActive = manga.IsActive,
                Genres = manga.MangaGenres
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
                ChapterCount = manga.Chapters.Count(c => c.IsActive)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

