using MediatR;
using Microsoft.AspNetCore.Mvc;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Author.Queries;

namespace SkyHighManga.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách authors
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AuthorDto>>> GetAuthors([FromQuery] string? searchTerm = null)
    {
        var query = new GetAuthorsQuery { SearchTerm = searchTerm };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy author theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AuthorDto>> GetAuthorById(Guid id)
    {
        var query = new GetAuthorByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
        
        return Ok(result);
    }
}

