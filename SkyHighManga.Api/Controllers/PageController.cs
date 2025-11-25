using MediatR;
using Microsoft.AspNetCore.Mvc;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Features.Page.Queries;

namespace SkyHighManga.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PageController : ControllerBase
{
    private readonly IMediator _mediator;

    public PageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách pages của một chapter
    /// </summary>
    [HttpGet("chapter/{chapterId}")]
    public async Task<ActionResult<List<PageDto>>> GetPagesByChapterId(Guid chapterId)
    {
        var query = new GetPagesByChapterIdQuery { ChapterId = chapterId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

