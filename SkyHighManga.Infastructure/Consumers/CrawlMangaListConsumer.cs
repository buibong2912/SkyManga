using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyHighManga.Application.Contracts;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Consumers;

/// <summary>
/// Consumer để crawl danh sách manga từ search pages
/// Sau đó publish messages cho từng manga để crawl details
/// </summary>
public class CrawlMangaListConsumer : IConsumer<CrawlMangaListCommand>
{
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CrawlMangaListConsumer> _logger;

    public CrawlMangaListConsumer(
        ICrawlerFactory crawlerFactory,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CrawlMangaListConsumer> logger)
    {
        _crawlerFactory = crawlerFactory;
        _context = context;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CrawlMangaListCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("🚀 [CrawlMangaListConsumer] Bắt đầu crawl danh sách manga từ {BaseSearchUrl}, MaxPages: {MaxPages}", 
            command.BaseSearchUrl, command.MaxPages?.ToString() ?? "null (tất cả)");

        try
        {
            // Lấy source
            var source = await _context.Sources.FindAsync(new object[] { command.SourceId }, context.CancellationToken);
            if (source == null)
            {
                _logger.LogError("❌ [CrawlMangaListConsumer] Không tìm thấy source với ID {SourceId}", command.SourceId);
                return;
            }
            _logger.LogInformation("✅ [CrawlMangaListConsumer] Đã tìm thấy source: {SourceName}", source.Name);

            // Lấy crawl job
            var crawlJob = await _unitOfWork.CrawlJobs.GetByIdAsync(command.CrawlJobId, context.CancellationToken);
            if (crawlJob == null)
            {
                _logger.LogError("❌ [CrawlMangaListConsumer] Không tìm thấy crawl job với ID {CrawlJobId}", command.CrawlJobId);
                return;
            }
            _logger.LogInformation("✅ [CrawlMangaListConsumer] Đã tìm thấy crawl job: {CrawlJobName}", crawlJob.Name);

            // Tạo manga crawler
            var mangaCrawler = _crawlerFactory.CreateMangaCrawler(source);
            if (mangaCrawler == null)
            {
                _logger.LogError("❌ [CrawlMangaListConsumer] Không thể tạo manga crawler cho source {SourceId}", command.SourceId);
                return;
            }
            _logger.LogInformation("✅ [CrawlMangaListConsumer] Đã tạo manga crawler");

            // Crawl danh sách manga từ search pages
            var crawlerContext = new SkyHighManga.Application.Common.Models.CrawlerContext
            {
                Source = source,
                CrawlJob = crawlJob,
                StartUrl = command.BaseSearchUrl,
                CancellationToken = context.CancellationToken
            };

            _logger.LogInformation("📥 [CrawlMangaListConsumer] Bắt đầu gọi SearchMangaAsync với maxPages = {MaxPages}", 
                command.MaxPages?.ToString() ?? "null");
            
            var searchResult = await mangaCrawler.SearchMangaAsync(
                "",
                crawlerContext,
                maxResults: null,
                maxPages: command.MaxPages);
            
            _logger.LogInformation("📤 [CrawlMangaListConsumer] SearchMangaAsync đã hoàn thành. IsSuccess: {IsSuccess}, ErrorMessage: {ErrorMessage}", 
                searchResult.IsSuccess, searchResult.ErrorMessage ?? "null");

            if (!searchResult.IsSuccess || searchResult.Data == null)
            {
                _logger.LogError("❌ [CrawlMangaListConsumer] Lỗi khi crawl danh sách manga: {ErrorMessage}", searchResult.ErrorMessage);
                return;
            }

            var mangas = searchResult.Data.ToList();
            _logger.LogInformation("✅ [CrawlMangaListConsumer] Tìm thấy {Count} mangas, đang publish messages để crawl details", mangas.Count);
            
            if (mangas.Count == 0)
            {
                _logger.LogWarning("⚠️ [CrawlMangaListConsumer] Không tìm thấy manga nào! Có thể:");
                _logger.LogWarning("   1. maxPages = null nhưng pagination không được parse đúng");
                _logger.LogWarning("   2. Trang search không có kết quả");
                _logger.LogWarning("   3. Có lỗi trong quá trình crawl");
                return;
            }

            // Publish messages cho từng manga để crawl details song song
            // Publish tất cả cùng lúc để tối ưu tốc độ (không chờ từng batch)
            _logger.LogInformation("Đang publish {Count} mangas song song...", mangas.Count);

            var publishTasks = mangas.Select(async manga =>
            {
                await _publishEndpoint.Publish(new CrawlMangaCommand
                {
                    SourceId = command.SourceId,
                    CrawlJobId = command.CrawlJobId,
                    MangaUrl = manga.SourceUrl,
                    MangaTitle = manga.Title ?? "Unknown",
                    SkipExisting = true
                }, context.CancellationToken);
            });

            // Publish tất cả cùng lúc, không chờ từng batch
            await Task.WhenAll(publishTasks);

            _logger.LogInformation("✅ Đã publish tất cả {Count} messages để crawl manga details", mangas.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý CrawlMangaListCommand");
            throw;
        }
    }
}

