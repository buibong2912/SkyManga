using System.Threading;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Chapter.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Chapter.Queries;

public class GetChaptersByMangaIdQueryHandler : IRequestHandler<GetChaptersByMangaIdQuery, List<ChapterDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetChaptersByMangaIdQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetChaptersByMangaIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetChaptersByMangaIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<List<ChapterDto>> Handle(GetChaptersByMangaIdQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var chapters = await _unitOfWork.Chapters.GetByMangaIdAsync(request.MangaId, cancellationToken);

            return chapters
                .Where(c => c.IsActive)
                .OrderBy(c => c.ChapterIndex ?? int.MaxValue)
                .Select(c => new ChapterDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    ChapterNumber = c.ChapterNumber,
                    ChapterIndex = c.ChapterIndex,
                    PageCount = c.PageCount,
                    MangaId = c.MangaId,
                    MangaTitle = c.Manga?.Title,
                    SourceChapterId = c.SourceChapterId,
                    SourceUrl = c.SourceUrl,
                    CountView = c.CountView,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    PublishedAt = c.PublishedAt,
                    IsActive = c.IsActive
                })
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

