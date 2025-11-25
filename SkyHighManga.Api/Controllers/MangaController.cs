using MediatR;
using Microsoft.AspNetCore.Mvc;
using SkyHighManga.Application.Common.DTOs;
using SkyHighManga.Application.Common.Responses;
using SkyHighManga.Application.Features.Manga.Commands;
using SkyHighManga.Application.Features.Manga.Queries;

namespace SkyHighManga.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MangaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MangaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách manga với phân trang và filter
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MangaDto>>> GetMangas(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Domain.Entities.MangaStatus? status = null,
        [FromQuery] Guid? authorId = null,
        [FromQuery] Guid? genreId = null,
        [FromQuery] string? sortBy = "UpdatedAt",
        [FromQuery] bool sortDescending = true)
    {
        var query = new GetMangasQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = status,
            AuthorId = authorId,
            GenreId = genreId,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy manga theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MangaDto>> GetMangaById(Guid id)
    {
        var query = new GetMangaByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
        
        return Ok(result);
    }

    /// <summary>
    /// Tạo manga mới
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MangaDto>> CreateManga([FromBody] CreateMangaCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetMangaById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Cập nhật manga
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MangaDto>> UpdateManga(Guid id, [FromBody] UpdateMangaCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Xóa manga (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteManga(Guid id)
    {
        var command = new DeleteMangaCommand { Id = id };
        var result = await _mediator.Send(command);
        
        if (!result)
            return NotFound();
        
        return NoContent();
    }
}

