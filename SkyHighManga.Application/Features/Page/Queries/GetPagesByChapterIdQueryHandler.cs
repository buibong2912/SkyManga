using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Page.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Page.Queries;

public class GetPagesByChapterIdQueryHandler : IRequestHandler<GetPagesByChapterIdQuery, List<PageDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPagesByChapterIdQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetPagesByChapterIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetPagesByChapterIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<List<PageDto>> Handle(GetPagesByChapterIdQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var pages = await _unitOfWork.Pages.FindAsync(
                p => p.ChapterId == request.ChapterId && p.IsActive,
                cancellationToken);

            return pages
                .OrderBy(p => p.PageNumber)
                .Select(p => new PageDto
                {
                    Id = p.Id,
                    PageNumber = p.PageNumber,
                    ImageUrl = p.ImageUrl,
                    LocalFilePath = p.LocalFilePath,
                    FileSize = p.FileSize,
                    ImageFormat = p.ImageFormat,
                    Width = p.Width,
                    Height = p.Height,
                    ChapterId = p.ChapterId,
                    SourcePageId = p.SourcePageId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsDownloaded = p.IsDownloaded,
                    IsActive = p.IsActive
                })
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

