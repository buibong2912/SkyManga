using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;
using SkyHighManga.Infastructure.Jobs;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlJobController : ControllerBase
{
    private readonly ICrawlJobService _crawlJobService;
    private readonly ICrawlJobOrchestratorService _orchestratorService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CrawlJobController> _logger;

    public CrawlJobController(
        ICrawlJobService crawlJobService,
        ICrawlJobOrchestratorService orchestratorService,
        ApplicationDbContext context,
        ILogger<CrawlJobController> logger)
    {
        _crawlJobService = crawlJobService;
        _orchestratorService = orchestratorService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Crawl tất cả mangas từ search page (sử dụng MassTransit - nhanh hơn)
    /// </summary>
    [HttpPost("crawl-all-mangas-async")]
    public async Task<IActionResult> CrawlAllMangasAsync(
        [FromQuery] string? sourceName = "Nettruyen",
        [FromQuery] int? maxPages = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = await _context.Sources
                .FirstOrDefaultAsync(s => s.Name == sourceName, cancellationToken);

            if (source == null)
            {
                return NotFound($"Không tìm thấy source: {sourceName}");
            }

            _logger.LogInformation("Bắt đầu crawl all mangas (async) từ source: {SourceName}, MaxPages: {MaxPages}", 
                sourceName, maxPages);

            var crawlJobId = await _orchestratorService.StartCrawlAllMangasAsync(
                source.Id,
                maxPages,
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Đã bắt đầu crawl job",
                crawlJobId = crawlJobId,
                note = "Sử dụng MassTransit để crawl song song, kiểm tra tiến độ tại /api/crawljob/jobs/{crawlJobId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắt đầu crawl all mangas (async)");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Crawl chapters và pages cho tất cả mangas đã có trong database (đa luồng cao)
    /// </summary>
    [HttpPost("crawl-all-mangas-chapters")]
    public async Task<IActionResult> CrawlAllMangasChapters(
        [FromQuery] string? sourceName = "Nettruyen",
        [FromQuery] int? maxMangas = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = await _context.Sources
                .FirstOrDefaultAsync(s => s.Name == sourceName, cancellationToken);

            if (source == null)
            {
                return NotFound($"Không tìm thấy source: {sourceName}");
            }

            _logger.LogInformation("Bắt đầu crawl chapters cho tất cả mangas (đa luồng cao) từ source: {SourceName}", sourceName);

            var crawlJobId = await _orchestratorService.StartCrawlAllMangasChaptersAsync(
                source.Id,
                maxMangas,
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Đã bắt đầu crawl chapters cho tất cả mangas (đa luồng cao)",
                crawlJobId = crawlJobId,
                note = "Sử dụng MassTransit với concurrency cao để crawl chapters và pages song song, kiểm tra tiến độ tại /api/crawljob/jobs/{crawlJobId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắt đầu crawl all mangas chapters");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Crawl tất cả mangas từ search page (sử dụng Hangfire - có thể schedule)
    /// </summary>
    [HttpPost("crawl-all-mangas-background")]
    public async Task<IActionResult> CrawlAllMangasBackground(
        [FromQuery] string? sourceName = "Nettruyen",
        [FromQuery] int? maxPages = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = await _context.Sources
                .FirstOrDefaultAsync(s => s.Name == sourceName, cancellationToken);

            if (source == null)
            {
                return NotFound($"Không tìm thấy source: {sourceName}");
            }

            _logger.LogInformation("Bắt đầu crawl all mangas (background) từ source: {SourceName}, MaxPages: {MaxPages}", 
                sourceName, maxPages);

            // Sử dụng instance method với DI
            var jobId = BackgroundJob.Enqueue<CrawlJobs>(x => x.CrawlAllMangasJob(source.Id, maxPages));

            return Ok(new
            {
                success = true,
                message = "Đã queue crawl job vào Hangfire",
                hangfireJobId = jobId,
                note = "Kiểm tra tiến độ tại /hangfire"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi queue crawl all mangas (background)");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Crawl tất cả mangas từ search page (synchronous - chậm hơn)
    /// </summary>
    [HttpPost("crawl-all-mangas")]
    public async Task<IActionResult> CrawlAllMangas(
        [FromQuery] string? sourceName = "Nettruyen",
        [FromQuery] int? maxPages = null,
        [FromQuery] int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = await _context.Sources
                .FirstOrDefaultAsync(s => s.Name == sourceName, cancellationToken);

            if (source == null)
            {
                return NotFound($"Không tìm thấy source: {sourceName}");
            }

            _logger.LogInformation("Bắt đầu crawl all mangas từ source: {SourceName}, MaxPages: {MaxPages}", 
                sourceName, maxPages);

            var result = await _crawlJobService.CrawlAllMangasAsync(
                source,
                crawlJob: null,
                maxPages: maxPages,
                maxConcurrency: maxConcurrency,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = "Crawl thành công",
                    totalMangas = result.Data,
                    elapsedMilliseconds = result.ElapsedMilliseconds
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage,
                exception = result.Exception?.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi crawl all mangas");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Crawl một manga cụ thể với chapters và pages
    /// </summary>
    [HttpPost("crawl-manga")]
    public async Task<IActionResult> CrawlManga(
        [FromQuery] string mangaUrl,
        [FromQuery] string? sourceName = "Nettruyen",
        [FromQuery] bool skipExisting = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(mangaUrl))
            {
                return BadRequest("mangaUrl là bắt buộc");
            }

            var source = await _context.Sources
                .FirstOrDefaultAsync(s => s.Name == sourceName, cancellationToken);

            if (source == null)
            {
                return NotFound($"Không tìm thấy source: {sourceName}");
            }

            _logger.LogInformation("Bắt đầu crawl manga: {MangaUrl}", mangaUrl);

            var result = await _crawlJobService.CrawlMangaFullAsync(
                source,
                mangaUrl,
                crawlJob: null,
                skipExisting: skipExisting,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = "Crawl manga thành công",
                    mangaId = result.Data,
                    elapsedMilliseconds = result.ElapsedMilliseconds
                });
            }

            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage,
                exception = result.Exception?.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi crawl manga: {MangaUrl}", mangaUrl);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách crawl jobs
    /// </summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetCrawlJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await _context.CrawlJobs
                .Include(j => j.Source)
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new
                {
                    j.Id,
                    j.Name,
                    j.Type,
                    j.Status,
                    j.TotalItems,
                    j.ProcessedItems,
                    j.SuccessItems,
                    j.FailedItems,
                    j.StartedAt,
                    j.CompletedAt,
                    j.Duration,
                    SourceName = j.Source.Name,
                    j.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var total = await _context.CrawlJobs.CountAsync(cancellationToken);

            return Ok(new
            {
                data = jobs,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách crawl jobs");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết một crawl job
    /// </summary>
    [HttpGet("jobs/{id}")]
    public async Task<IActionResult> GetCrawlJob(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var job = await _context.CrawlJobs
                .Include(j => j.Source)
                .Include(j => j.Logs.OrderByDescending(l => l.CreatedAt).Take(100))
                .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

            if (job == null)
            {
                return NotFound($"Không tìm thấy crawl job với ID: {id}");
            }

            return Ok(new
            {
                job.Id,
                job.Name,
                job.Description,
                job.Type,
                job.Status,
                job.TotalItems,
                job.ProcessedItems,
                job.SuccessItems,
                job.FailedItems,
                job.StartUrl,
                job.StartedAt,
                job.CompletedAt,
                job.Duration,
                job.ErrorMessage,
                SourceName = job.Source.Name,
                Logs = job.Logs.Select(l => new
                {
                    l.Id,
                    l.Message,
                    l.Level,
                    l.Url,
                    l.CreatedAt
                }),
                job.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết crawl job: {JobId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

