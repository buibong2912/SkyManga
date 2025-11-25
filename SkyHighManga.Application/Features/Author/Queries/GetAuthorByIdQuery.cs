using MediatR;
using SkyHighManga.Application.Common.DTOs;

namespace SkyHighManga.Application.Features.Author.Queries;

public class GetAuthorByIdQuery : IRequest<AuthorDto?>
{
    public Guid Id { get; set; }
}

