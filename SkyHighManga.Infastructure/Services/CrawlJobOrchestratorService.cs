using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Contracts;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Services;

/// <summary>
/// Service để orchestrate crawl jobs sử dụng MassTransit
/// </summary>
public interface ICrawlJobOrchestratorService
{
    /// <summary>
    /// Bắt đầu crawl tất cả mangas từ search pages
    /// </summary>
    Task<Guid> StartCrawlAllMangasAsync(
        Guid sourceId,
        int? maxPages = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crawl chapters và pages cho tất cả mangas đã có trong database
    /// Sử dụng khi đã có list manga, muốn crawl chapters và pages với đa luồng cao
    /// </summary>
    Task<Guid> StartCrawlAllMangasChaptersAsync(
        Guid sourceId,
        int? maxMangas = null,
        CancellationToken cancellationToken = default);
}

public class CrawlJobOrchestratorService : ICrawlJobOrchestratorService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CrawlJobOrchestratorService> _logger;

    public CrawlJobOrchestratorService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CrawlJobOrchestratorService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Guid> StartCrawlAllMangasAsync(
        Guid sourceId,
        int? maxPages = null,
        CancellationToken cancellationToken = default)
    {
        // Lấy source
        var source = await _context.Sources.FindAsync(new object[] { sourceId }, cancellationToken);
        if (source == null)
        {
            throw new ArgumentException($"Không tìm thấy source với ID {sourceId}", nameof(sourceId));
        }

        // Tạo crawl job
        var crawlJob = new CrawlJob
        {
            Id = Guid.NewGuid(),
            Name = $"Crawl toàn bộ mangas từ {source.BaseUrl}",
            Type = CrawlJobType.FullCrawl,
            Status = CrawlJobStatus.Pending,
            SourceId = sourceId,
            StartUrl = $"{source.BaseUrl}/tim-kiem",
            MaxPages = maxPages,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            _context.CrawlJobs.Add(crawlJob);
            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }

        _logger.LogInformation("✅ [Orchestrator] Đã tạo crawl job {CrawlJobId} cho source {SourceId}, MaxPages: {MaxPages}", 
            crawlJob.Id, sourceId, maxPages?.ToString() ?? "null (tất cả)");

        // Publish message để bắt đầu crawl
        var command = new CrawlMangaListCommand
        {
            SourceId = sourceId,
            CrawlJobId = crawlJob.Id,
            MaxPages = maxPages,
            BaseSearchUrl = $"{source.BaseUrl}/tim-kiem"
        };
        
        _logger.LogInformation("📤 [Orchestrator] Đang publish CrawlMangaListCommand: SourceId={SourceId}, CrawlJobId={CrawlJobId}, MaxPages={MaxPages}, BaseSearchUrl={BaseSearchUrl}", 
            command.SourceId, command.CrawlJobId, command.MaxPages?.ToString() ?? "null", command.BaseSearchUrl);
        
        await _publishEndpoint.Publish(command, cancellationToken);

        _logger.LogInformation("✅ [Orchestrator] Đã publish CrawlMangaListCommand cho crawl job {CrawlJobId}. Consumer sẽ xử lý message này.", crawlJob.Id);

        return crawlJob.Id;
    }

    public async Task<Guid> StartCrawlAllMangasChaptersAsync(
        Guid sourceId,
        int? maxMangas = null,
        CancellationToken cancellationToken = default)
    {
        // Lấy source
        var source = await _context.Sources.FindAsync(new object[] { sourceId }, cancellationToken);
        if (source == null)
        {
            throw new ArgumentException($"Không tìm thấy source với ID {sourceId}", nameof(sourceId));
        }

        // Tạo crawl job
        var crawlJob = new CrawlJob
        {
            Id = Guid.NewGuid(),
            Name = $"Crawl chapters cho tất cả mangas từ {source.BaseUrl}",
            Type = CrawlJobType.UpdateManga,
            Status = CrawlJobStatus.Pending,
            SourceId = sourceId,
            StartUrl = $"{source.BaseUrl}/tim-kiem",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(cancellationToken);
        try
        {
            _context.CrawlJobs.Add(crawlJob);
            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
        }

        _logger.LogInformation("Đã tạo crawl job {CrawlJobId} để crawl chapters cho mangas đã có trong DB", crawlJob.Id);

        // Publish message để crawl chapters cho tất cả mangas
        await _publishEndpoint.Publish(new CrawlAllMangasChaptersCommand
        {
            SourceId = sourceId,
            CrawlJobId = crawlJob.Id,
            MaxMangas = maxMangas,
            SkipExisting = true
        }, cancellationToken);

        _logger.LogInformation("Đã publish CrawlAllMangasChaptersCommand cho crawl job {CrawlJobId}", crawlJob.Id);

        return crawlJob.Id;
    }
}

