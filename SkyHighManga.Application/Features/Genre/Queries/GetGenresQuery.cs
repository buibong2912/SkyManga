using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Genre.Queries;

public class GetGenresQuery : IRequest<List<GenreDto>>
{
    public string? SearchTerm { get; set; }
}

