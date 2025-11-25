using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Chapter.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Chapter.Queries;

public class GetChapterByIdQueryHandler : IRequestHandler<GetChapterByIdQuery, ChapterDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetChapterByIdQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetChapterByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetChapterByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<ChapterDto?> Handle(GetChapterByIdQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(request.Id, cancellationToken);
            if (chapter == null || !chapter.IsActive)
                return null;

            return new ChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                ChapterNumber = chapter.ChapterNumber,
                ChapterIndex = chapter.ChapterIndex,
                PageCount = chapter.PageCount,
                MangaId = chapter.MangaId,
                MangaTitle = chapter.Manga?.Title,
                SourceChapterId = chapter.SourceChapterId,
                SourceUrl = chapter.SourceUrl,
                CountView = chapter.CountView,
                CreatedAt = chapter.CreatedAt,
                UpdatedAt = chapter.UpdatedAt,
                PublishedAt = chapter.PublishedAt,
                IsActive = chapter.IsActive
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

