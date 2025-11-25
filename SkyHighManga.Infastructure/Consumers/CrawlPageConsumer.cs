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
/// Consumer để crawl và lưu pages cho một chapter
/// </summary>
public class CrawlPageConsumer : IConsumer<CrawlPageCommand>
{
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly IMangaService _mangaService;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CrawlPageConsumer> _logger;

    public CrawlPageConsumer(
        ICrawlerFactory crawlerFactory,
        IMangaService mangaService,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<CrawlPageConsumer> logger)
    {
        _crawlerFactory = crawlerFactory;
        _mangaService = mangaService;
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CrawlPageCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Bắt đầu crawl pages cho chapter {ChapterId}", command.ChapterId);

        try
        {
            // Lấy source
            var source = await _context.Sources.FindAsync(new object[] { command.SourceId }, context.CancellationToken);
            if (source == null)
            {
                _logger.LogError("Không tìm thấy source với ID {SourceId}", command.SourceId);
                return;
            }

            // Lấy crawl job
            var crawlJob = await _unitOfWork.CrawlJobs.GetByIdAsync(command.CrawlJobId, context.CancellationToken);
            if (crawlJob == null)
            {
                _logger.LogError("Không tìm thấy crawl job với ID {CrawlJobId}", command.CrawlJobId);
                return;
            }

            // Tạo page crawler
            var pageCrawler = _crawlerFactory.CreatePageCrawler(source);
            if (pageCrawler == null)
            {
                _logger.LogError("Không thể tạo page crawler cho source {SourceId}", command.SourceId);
                return;
            }

            var crawlerContext = new SkyHighManga.Application.Common.Models.CrawlerContext
            {
                Source = source,
                CrawlJob = crawlJob,
                StartUrl = command.ChapterUrl,
                CancellationToken = context.CancellationToken
            };

            // Crawl page URLs
            var pagesResult = await pageCrawler.CrawlPageUrlsAsync(command.ChapterUrl, crawlerContext);
            if (!pagesResult.IsSuccess || pagesResult.Data == null)
            {
                _logger.LogError("Lỗi khi crawl pages cho chapter {ChapterUrl}: {ErrorMessage}", command.ChapterUrl, pagesResult.ErrorMessage);
                return;
            }

            var pageUrls = pagesResult.Data.ToList();
            _logger.LogInformation("Tìm thấy {Count} pages cho chapter {ChapterId}", pageUrls.Count, command.ChapterId);

            // Lưu pages (thứ tự đã được đảm bảo trong SavePagesAsync)
            var savedCount = await _mangaService.SavePagesAsync(command.ChapterId, pageUrls, context.CancellationToken);
            _logger.LogInformation("Đã lưu {SavedCount} pages cho chapter {ChapterId}", savedCount, command.ChapterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý CrawlPageCommand cho chapter {ChapterId}", command.ChapterId);
            throw;
        }
    }
}

