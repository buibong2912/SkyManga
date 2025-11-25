using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Author.Queries;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Common;

namespace SkyHighManga.Application.Features.Author.Queries;

public class GetAuthorsQueryHandler : IRequestHandler<GetAuthorsQuery, List<AuthorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAuthorsQueryHandler> _logger;
    private readonly SemaphoreSlim _semaphore;

    public GetAuthorsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAuthorsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _semaphore = DbContextSemaphore.Instance;
    }

    public async Task<List<AuthorDto>> Handle(GetAuthorsQuery request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var authors = await _unitOfWork.Authors.GetAllAsync(cancellationToken);
            
            var query = authors.Where(a => a.IsActive).AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(a => 
                    a.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (a.AlternativeName != null && a.AlternativeName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            return query
                .OrderBy(a => a.Name)
                .Select(a => new AuthorDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    AlternativeName = a.AlternativeName,
                    Bio = a.Bio,
                    ProfileImageUrl = a.ProfileImageUrl,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    IsActive = a.IsActive,
                    MangaCount = a.Mangas.Count(m => m.IsActive)
                })
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

