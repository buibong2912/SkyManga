using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Genre.Queries;

public class GetGenreByIdQuery : IRequest<GenreDto?>
{
    public Guid Id { get; set; }
}

