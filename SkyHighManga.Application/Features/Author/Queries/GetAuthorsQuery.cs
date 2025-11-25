using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Author.Queries;

public class GetAuthorsQuery : IRequest<List<AuthorDto>>
{
    public string? SearchTerm { get; set; }
}

