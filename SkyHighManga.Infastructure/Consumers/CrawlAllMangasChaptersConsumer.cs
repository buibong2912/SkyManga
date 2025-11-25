using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Contracts;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Consumers;

/// <summary>
/// Consumer để crawl chapters cho tất cả mangas đã có trong database
/// Tối ưu đa luồng cao để crawl nhanh
/// </summary>
public class CrawlAllMangasChaptersConsumer : IConsumer<CrawlAllMangasChaptersCommand>
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CrawlAllMangasChaptersConsumer> _logger;

    public CrawlAllMangasChaptersConsumer(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CrawlAllMangasChaptersConsumer> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CrawlAllMangasChaptersCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Bắt đầu crawl chapters cho tất cả mangas từ source {SourceId}", command.SourceId);

        try
        {
            // Lấy tất cả mangas từ database
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(context.CancellationToken);
            List<Manga> mangas;
            try
            {
                var query = _context.Mangas.Where(m => m.SourceId == command.SourceId);
                
                if (command.MaxMangas.HasValue)
                {
                    query = query.Take(command.MaxMangas.Value);
                }

                mangas = await query.ToListAsync(context.CancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }

            _logger.LogInformation("Tìm thấy {Count} mangas trong database, đang publish messages để crawl chapters", mangas.Count);

            // Publish messages cho từng manga để crawl chapters song song
            // Publish tất cả cùng lúc để tối ưu tốc độ (không chờ từng batch)
            var publishTasks = mangas.Select(async manga =>
            {
                await _publishEndpoint.Publish(new CrawlMangaCommand
                {
                    SourceId = command.SourceId,
                    CrawlJobId = command.CrawlJobId,
                    MangaUrl = manga.SourceUrl,
                    MangaTitle = manga.Title ?? "Unknown",
                    SkipExisting = command.SkipExisting
                }, context.CancellationToken);
            });

            // Publish tất cả cùng lúc, không chờ từng batch
            await Task.WhenAll(publishTasks);

            _logger.LogInformation("✅ Đã publish tất cả {Count} messages để crawl chapters cho mangas đã có trong DB", mangas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý CrawlAllMangasChaptersCommand");
            throw;
        }
    }
}

