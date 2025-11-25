using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Manga.Commands;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Manga.Commands;

public class CreateMangaCommandHandler : IRequestHandler<CreateMangaCommand, MangaDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateMangaCommandHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public CreateMangaCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateMangaCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<MangaDto> Handle(CreateMangaCommand request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var manga = new Domain.Entities.Manga
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                AlternativeTitle = request.AlternativeTitle,
                Description = request.Description,
                CoverImageUrl = request.CoverImageUrl,
                ThumbnailUrl = request.ThumbnailUrl,
                Status = request.Status,
                YearOfRelease = request.YearOfRelease,
                OriginalLanguage = request.OriginalLanguage,
                AuthorId = request.AuthorId,
                SourceId = request.SourceId,
                SourceMangaId = request.SourceMangaId,
                SourceUrl = request.SourceUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.Mangas.AddAsync(manga, cancellationToken);

            // Add genres
            if (request.GenreIds.Any())
            {
                foreach (var genreId in request.GenreIds)
                {
                    var genre = await _unitOfWork.Genres.GetByIdAsync(genreId, cancellationToken);
                    if (genre != null && genre.IsActive)
                    {
                        manga.MangaGenres.Add(new MangaGenre
                        {
                            Id = Guid.NewGuid(),
                            MangaId = manga.Id,
                            GenreId = genreId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload with relationships
            var createdManga = await _unitOfWork.Mangas.GetFullAsync(manga.Id, cancellationToken);
            if (createdManga == null)
                throw new InvalidOperationException("Failed to create manga");

            return MapToDto(createdManga);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static MangaDto MapToDto(Domain.Entities.Manga manga)
    {
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
}

