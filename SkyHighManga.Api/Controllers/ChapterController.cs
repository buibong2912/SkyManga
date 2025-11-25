using MediatR;
using Microsoft.AspNetCore.Mvc;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Chapter.Queries;

namespace SkyHighManga.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChapterController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChapterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy chapter theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChapterDto>> GetChapterById(Guid id)
    {
        var query = new GetChapterByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
        
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách chapters của một manga
    /// </summary>
    [HttpGet("manga/{mangaId}")]
    public async Task<ActionResult<List<ChapterDto>>> GetChaptersByMangaId(Guid mangaId)
    {
        var query = new GetChaptersByMangaIdQuery { MangaId = mangaId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

