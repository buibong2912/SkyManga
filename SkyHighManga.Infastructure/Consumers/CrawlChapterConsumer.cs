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
/// Consumer để crawl chapter và publish message để crawl pages
/// </summary>
public class CrawlChapterConsumer : IConsumer<CrawlChapterCommand>
{
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly IMangaService _mangaService;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CrawlChapterConsumer> _logger;

    public CrawlChapterConsumer(
        ICrawlerFactory crawlerFactory,
        IMangaService mangaService,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CrawlChapterConsumer> logger)
    {
        _crawlerFactory = crawlerFactory;
        _mangaService = mangaService;
        _context = context;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CrawlChapterCommand> context)
    {
        var command = context.Message;
        _logger.LogInformation("Bắt đầu crawl chapter: {ChapterTitle} ({ChapterUrl})", command.ChapterTitle, command.ChapterUrl);

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

            // Kiểm tra manga tồn tại trước khi lưu chapter (tránh foreign key violation)
            await SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.WaitAsync(context.CancellationToken);
            Manga? manga;
            try
            {
                manga = await _unitOfWork.Mangas.GetByIdAsync(command.MangaId, context.CancellationToken);
            }
            finally
            {
                SkyHighManga.Infastructure.Data.DbContextSemaphore.Instance.Release();
            }
            
            if (manga == null)
            {
                _logger.LogWarning("Manga với ID {MangaId} chưa tồn tại, sẽ retry sau 5 giây...", command.MangaId);
                // Throw exception để trigger retry policy (sẽ retry sau 5 giây theo cấu hình)
                throw new InvalidOperationException($"Manga với ID {command.MangaId} chưa tồn tại trong database. Có thể manga đang được lưu bởi CrawlMangaConsumer. Sẽ retry sau.");
            }

            // Kiểm tra nếu chapter đã tồn tại
            if (command.SkipExisting && !string.IsNullOrEmpty(command.SourceChapterId))
            {
                var chapterExists = await _mangaService.ChapterExistsAsync(command.MangaId, command.SourceChapterId, context.CancellationToken);
                if (chapterExists)
                {
                    _logger.LogInformation("Chapter {ChapterTitle} đã tồn tại, bỏ qua", command.ChapterTitle);
                    return;
                }
            }

            /*/*
            // Tạo chapter crawler để lấy chapter data
            var chapterCrawler = _crawlerFactory.CreateChapterCrawler(source);
            if (chapterCrawler == null)
            {
                _logger.LogError("Không thể tạo chapter crawler cho source {SourceId}", command.SourceId);
                return;
            }
            #1#



            // Lưu chapter*/
            var chapterData = new SkyHighManga.Application.Common.Models.ChapterCrawlData
            {
                SourceUrl = command.ChapterUrl,
                Title = command.ChapterTitle,
                SourceChapterId = command.SourceChapterId
            };

            var savedChapter = await _mangaService.SaveOrUpdateChapterAsync(chapterData, command.MangaId, context.CancellationToken);
            _logger.LogInformation("Đã lưu chapter: {ChapterTitle}", savedChapter.Title);

            // Publish message để crawl pages
            await _publishEndpoint.Publish(new CrawlPageCommand
            {
                SourceId = command.SourceId,
                CrawlJobId = command.CrawlJobId,
                ChapterId = savedChapter.Id,
                ChapterUrl = command.ChapterUrl,
                SkipExisting = command.SkipExisting
            }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý CrawlChapterCommand cho {ChapterUrl}", command.ChapterUrl);
            throw;
        }
    }
}

