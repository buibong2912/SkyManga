using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Author.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Author.Queries;

public class GetAuthorByIdQueryHandler : IRequestHandler<GetAuthorByIdQuery, AuthorDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAuthorByIdQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetAuthorByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAuthorByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<AuthorDto?> Handle(GetAuthorByIdQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var author = await _unitOfWork.Authors.GetByIdAsync(request.Id, cancellationToken);
            if (author == null || !author.IsActive)
                return null;

            return new AuthorDto
            {
                Id = author.Id,
                Name = author.Name,
                AlternativeName = author.AlternativeName,
                Bio = author.Bio,
                ProfileImageUrl = author.ProfileImageUrl,
                CreatedAt = author.CreatedAt,
                UpdatedAt = author.UpdatedAt,
                IsActive = author.IsActive,
                MangaCount = author.Mangas.Count(m => m.IsActive)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

