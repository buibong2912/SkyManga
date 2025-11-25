using MediatR;
using Microsoft.AspNetCore.Mvc;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Genre.Queries;

namespace SkyHighManga.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenreController : ControllerBase
{
    private readonly IMediator _mediator;

    public GenreController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách genres
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GenreDto>>> GetGenres([FromQuery] string? searchTerm = null)
    {
        var query = new GetGenresQuery { SearchTerm = searchTerm };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy genre theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GenreDto>> GetGenreById(Guid id)
    {
        var query = new GetGenreByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
        
        return Ok(result);
    }
}

