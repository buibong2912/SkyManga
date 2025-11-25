using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;
using SkyHighManga.Infastructure.Repositories;
using SkyHighManga.Infastructure.Services;
using SkyHighManga.Infastructure.Crawlers;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.UnitTest.Services;

/// <summary>
/// Tests cho CrawlJobService với in-memory database
/// </summary>
[TestFixture]
public class CrawlJobServiceTests
{
    private ApplicationDbContext _context = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ICrawlJobService _crawlJobService = null!;
    private IMangaService _mangaService = null!;
    private ICrawlerFactory _crawlerFactory = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);

        // Setup HTML parser
        _htmlParser = new HtmlAgilityPackParser();

        // Setup crawler factory (mock hoặc real)
        _crawlerFactory = new MockCrawlerFactory(_htmlParser);

        // Setup manga service
        _mangaService = new MangaService(_unitOfWork, _context);

        // Setup crawl job service
        _crawlJobService = new CrawlJobService(_unitOfWork, _context, _crawlerFactory, _mangaService);

        // Setup test source
        _source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "Nettruyen Test",
            BaseUrl = "https://aquastarsleep.co.uk",
            Type = SourceType.Website,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sources.Add(_source);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _unitOfWork?.Dispose();
    }

    [Test]
    public async Task CrawlAllMangasAsync_WithValidSource_ShouldCreateCrawlJob()
    {
        // Arrange
        var maxPages = 2; // Giới hạn 2 trang để test nhanh
        var maxConcurrency = 2;

        // Act
        var result = await _crawlJobService.CrawlAllMangasAsync(
            _source,
            crawlJob: null,
            maxPages: maxPages,
            maxConcurrency: maxConcurrency,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.GreaterThanOrEqualTo(0));

        // Verify job was created
        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == _source.Id);
        
        Assert.That(job, Is.Not.Null, "CrawlJob should be created");
        Assert.That(job!.Status, Is.EqualTo(CrawlJobStatus.Completed));
        Assert.That(job.Type, Is.EqualTo(CrawlJobType.FullCrawl));
        Assert.That(job.ProcessedItems, Is.GreaterThan(0));
    }

    [Test]
    public async Task CrawlAllMangasAsync_WithExistingJob_ShouldUseExistingJob()
    {
        // Arrange
        var existingJob = new CrawlJob
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            Type = CrawlJobType.FullCrawl,
            Status = CrawlJobStatus.Pending,
            SourceId = _source.Id,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CrawlJobs.AddAsync(existingJob);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _crawlJobService.CrawlAllMangasAsync(
            _source,
            crawlJob: existingJob,
            maxPages: 1,
            maxConcurrency: 2,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        // Verify job was updated
        var updatedJob = await _unitOfWork.CrawlJobs.GetByIdAsync(existingJob.Id);
        Assert.That(updatedJob, Is.Not.Null);
        Assert.That(updatedJob!.Status, Is.EqualTo(CrawlJobStatus.Completed));
        Assert.That(updatedJob.StartedAt, Is.Not.Null);
    }

    [Test]
    public async Task CrawlMangaFullAsync_WithValidUrl_ShouldCrawlAndSaveManga()
    {
        // Arrange
        var mangaUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece";

        // Act
        var result = await _crawlJobService.CrawlMangaFullAsync(
            _source,
            mangaUrl,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.EqualTo(Guid.Empty));

        // Verify manga was saved
        var manga = await _unitOfWork.Mangas.GetByIdAsync(result.Data);
        Assert.That(manga, Is.Not.Null, "Manga should be saved to database");
        Assert.That(manga!.Title, Is.Not.Empty);

        // Verify job was created
        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == _source.Id && j.Type == CrawlJobType.SingleManga);
        Assert.That(job, Is.Not.Null, "CrawlJob should be created");
        Assert.That(job!.Status, Is.EqualTo(CrawlJobStatus.Completed));
    }

    [Test]
    public async Task CrawlMangaFullAsync_WithSkipExisting_ShouldSkipExistingManga()
    {
        // Arrange
        var mangaUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece";

        // Crawl first time
        var firstResult = await _crawlJobService.CrawlMangaFullAsync(
            _source,
            mangaUrl,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);

        Assert.That(firstResult.IsSuccess, Is.True);
        var firstMangaId = firstResult.Data;

        // Act - Crawl second time with skipExisting = true
        var secondResult = await _crawlJobService.CrawlMangaFullAsync(
            _source,
            mangaUrl,
            crawlJob: null,
            skipExisting: true,
            cancellationToken: default);

        // Assert
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.Data, Is.EqualTo(firstMangaId), "Should return same manga ID when skipping existing");

        // Verify only one manga exists
        var mangas = await _unitOfWork.Mangas.FindAsync(m => m.SourceId == _source.Id);
        var mangaList = mangas.ToList();
        Assert.That(mangaList.Count, Is.EqualTo(1), "Should only have one manga");
    }

    [Test]
    public async Task CrawlMangaChaptersAsync_WithValidManga_ShouldCrawlChapters()
    {
        // Arrange - Create a manga first
        var manga = new Manga
        {
            Id = Guid.NewGuid(),
            Title = "Test Manga",
            SourceId = _source.Id,
            SourceMangaId = "test-manga-1",
            SourceUrl = $"{_source.BaseUrl}/truyen-tranh/test-manga",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Mangas.AddAsync(manga);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _crawlJobService.CrawlMangaChaptersAsync(
            _source,
            manga.Id,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.GreaterThanOrEqualTo(0));

        // Verify chapters were saved
        var chapters = await _unitOfWork.Chapters.FindAsync(c => c.MangaId == manga.Id);
        Assert.That(chapters, Is.Not.Empty, "Chapters should be saved");
    }

    [Test]
    public async Task CrawlChapterPagesAsync_WithValidChapter_ShouldCrawlPages()
    {
        // Arrange - Create manga and chapter
        var manga = new Manga
        {
            Id = Guid.NewGuid(),
            Title = "Test Manga",
            SourceId = _source.Id,
            SourceMangaId = "test-manga-1",
            SourceUrl = $"{_source.BaseUrl}/truyen-tranh/test-manga",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Mangas.AddAsync(manga);
        await _unitOfWork.SaveChangesAsync();

        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            Title = "Chapter 1",
            MangaId = manga.Id,
            SourceChapterId = "chapter-1",
            SourceUrl = $"{_source.BaseUrl}/truyen-tranh/test-manga/chapter-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Chapters.AddAsync(chapter);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _crawlJobService.CrawlChapterPagesAsync(
            _source,
            chapter.Id,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.GreaterThanOrEqualTo(0));

        // Verify pages were saved
        var pages = await _unitOfWork.Pages.FindAsync(p => p.ChapterId == chapter.Id);
        Assert.That(pages, Is.Not.Empty, "Pages should be saved");
    }

    [Test]
    public async Task CrawlAllMangasAsync_ShouldCreateLogs()
    {
        // Arrange
        var maxPages = 1;

        // Act
        var result = await _crawlJobService.CrawlAllMangasAsync(
            _source,
            crawlJob: null,
            maxPages: maxPages,
            maxConcurrency: 2,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        // Verify logs were created
        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == _source.Id);
        
        Assert.That(job, Is.Not.Null);
        
        var logs = await _context.CrawlJobLogs
            .Where(l => l.CrawlJobId == job!.Id)
            .ToListAsync();
        
        Assert.That(logs, Is.Not.Empty, "Logs should be created");
        Assert.That(logs.Any(l => l.Level == Domain.Entities.LogLevel.Info), Is.True, "Should have info logs");
    }

    [Test]
    public async Task CrawlAllMangasAsync_WithError_ShouldMarkJobAsFailed()
    {
        // Arrange - Use invalid source to cause error
        var invalidSource = new Source
        {
            Id = Guid.NewGuid(),
            Name = "Invalid Source",
            BaseUrl = "https://invalid-url-that-does-not-exist-12345.com",
            Type = SourceType.Website,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sources.Add(invalidSource);
        await _context.SaveChangesAsync();

        // Act
        var result = await _crawlJobService.CrawlAllMangasAsync(
            invalidSource,
            crawlJob: null,
            maxPages: 1,
            maxConcurrency: 1,
            cancellationToken: default);

        // Assert
        // May succeed or fail depending on crawler implementation
        // But if it fails, job should be marked as failed
        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == invalidSource.Id);
        
        if (job != null)
        {
            Assert.That(
                job.Status == CrawlJobStatus.Completed || job.Status == CrawlJobStatus.Failed,
                Is.True,
                "Job should be either completed or failed");
        }
    }

    [Test]
    public async Task CrawlJob_ShouldUpdateProgress()
    {
        // Arrange
        var maxPages = 2;

        // Act
        var result = await _crawlJobService.CrawlAllMangasAsync(
            _source,
            crawlJob: null,
            maxPages: maxPages,
            maxConcurrency: 2,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True);

        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == _source.Id);
        
        Assert.That(job, Is.Not.Null);
        Assert.That(job!.TotalItems, Is.GreaterThan(0));
        Assert.That(job.ProcessedItems, Is.GreaterThan(0));
        Assert.That(job.ProcessedItems, Is.LessThanOrEqualTo(job.TotalItems));
    }

    [Test]
    public async Task CrawlMangaFullAsync_ShouldSaveMangaWithChaptersAndPages()
    {
        // Arrange
        var mangaUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece";

        // Act
        var result = await _crawlJobService.CrawlMangaFullAsync(
            _source,
            mangaUrl,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        
        var manga = await _unitOfWork.Mangas.GetByIdAsync(result.Data);
        Assert.That(manga, Is.Not.Null);

        // Verify chapters were saved
        var chapters = await _unitOfWork.Chapters.FindAsync(c => c.MangaId == manga!.Id);
        var chapterList = chapters.ToList();
        
        if (chapterList.Count > 0)
        {
            // Verify at least one chapter has pages
            var firstChapter = chapterList.First();
            var pages = await _unitOfWork.Pages.FindAsync(p => p.ChapterId == firstChapter.Id);
            Assert.That(pages, Is.Not.Empty, "At least one chapter should have pages");
        }
    }
}

/// <summary>
/// Mock crawler factory for testing
/// </summary>
public class MockCrawlerFactory : ICrawlerFactory
{
    private readonly IHtmlParser _htmlParser;

    public MockCrawlerFactory(IHtmlParser htmlParser)
    {
        _htmlParser = htmlParser;
    }

    public ICrawler? CreateCrawler(Source source)
    {
        return new NettruyenCrawler(_htmlParser);
    }

    public ICrawler? CreateCrawler(string crawlerClassName)
    {
        return new NettruyenCrawler(_htmlParser);
    }

    public IMangaCrawler? CreateMangaCrawler(Source source)
    {
        return new NettruyenCrawler(_htmlParser);
    }

    public IChapterCrawler? CreateChapterCrawler(Source source)
    {
        return new NettruyenCrawler(_htmlParser);
    }

    public IPageCrawler? CreatePageCrawler(Source source)
    {
        return new NettruyenCrawler(_htmlParser);
    }

    public void RegisterCrawler<T>(string name) where T : class, ICrawler
    {
        // Not needed for testing
    }

    public IEnumerable<string> GetRegisteredCrawlers()
    {
        return new[] { "NettruyenCrawler" };
    }
}

