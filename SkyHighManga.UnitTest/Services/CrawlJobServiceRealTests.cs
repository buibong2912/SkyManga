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
/// Real tests cho CrawlJobService - crawl thật từ internet với in-memory database
/// </summary>
[TestFixture]
public class CrawlJobServiceRealTests
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

        // Setup real crawler factory
        _crawlerFactory = new RealCrawlerFactory(_htmlParser);

        // Setup manga service
        _mangaService = new MangaService(_unitOfWork, _context);

        // Setup crawl job service
        _crawlJobService = new CrawlJobService(_unitOfWork, _context, _crawlerFactory, _mangaService);

        // Setup test source - Nettruyen real URL
        _source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "Nettruyen",
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
        if (_context != null)
        {
            try
            {
                _context.Mangas.RemoveRange(_context.Mangas);
                _context.Chapters.RemoveRange(_context.Chapters);
                _context.Pages.RemoveRange(_context.Pages);
                _context.CrawlJobs.RemoveRange(_context.CrawlJobs);
                _context.CrawlJobLogs.RemoveRange(_context.CrawlJobLogs);
                _context.Authors.RemoveRange(_context.Authors);
                _context.Genres.RemoveRange(_context.Genres);
                _context.MangaGenres.RemoveRange(_context.MangaGenres);
                _context.SaveChanges();
            }
            catch
            {
            }
        }
        
        _context?.Dispose();
        _unitOfWork?.Dispose();
    }

    [Test]
    [Category("RealTest")]
    public async Task CrawlAllMangasAsync_RealCrawl_ShouldCrawlAndSaveMangas()
    {
        // Arrange
        var maxPages = 1; // Chỉ crawl 1 trang để test nhanh
        var maxConcurrency = 2;

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("REAL TEST: CrawlAllMangasAsync - Crawl thật từ internet");
        Console.WriteLine($"Source: {_source.BaseUrl}");
        Console.WriteLine($"Max Pages: {maxPages}");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawlJobService.CrawlAllMangasAsync(
            _source,
            crawlJob: null,
            maxPages: maxPages,
            maxConcurrency: maxConcurrency,
            cancellationToken: default);
        stopwatch.Stop();

        // Assert
        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("TEST RESULTS");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Mangas Crawled: {result.Data}");
        Console.WriteLine($"Time Taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine();

        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.GreaterThan(0), "Should crawl at least one manga");

        // Verify job was created
        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == _source.Id);
        
        Assert.That(job, Is.Not.Null, "CrawlJob should be created");
        Assert.That(job!.Status, Is.EqualTo(CrawlJobStatus.Completed));
        Assert.That(job.ProcessedItems, Is.GreaterThan(0));
        Assert.That(job.SuccessItems, Is.GreaterThan(0));

        Console.WriteLine($"Job Status: {job.Status}");
        Console.WriteLine($"Processed Items: {job.ProcessedItems}");
        Console.WriteLine($"Success Items: {job.SuccessItems}");
        Console.WriteLine($"Failed Items: {job.FailedItems}");
        Console.WriteLine();

        // Verify mangas were saved to database
        var mangas = await _unitOfWork.Mangas.FindAsync(m => m.SourceId == _source.Id);
        var mangaList = mangas.ToList();
        
        Console.WriteLine($"Mangas in Database: {mangaList.Count}");
        Assert.That(mangaList.Count, Is.GreaterThan(0), "Should have mangas in database");

        // Show sample mangas
        if (mangaList.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Sample Mangas:");
            foreach (var manga in mangaList)
            {
                Console.WriteLine($"  - {manga.Title}");
                Console.WriteLine($"    Source URL: {manga.SourceUrl}");
                Console.WriteLine($"    Source Manga ID: {manga.SourceMangaId}");
            }
        }

        // Verify logs were created
        var logs = await _context.CrawlJobLogs
            .Where(l => l.CrawlJobId == job.Id)
            .ToListAsync();
        
        Console.WriteLine();
        Console.WriteLine($"Logs Created: {logs.Count}");
        Assert.That(logs, Is.Not.Empty, "Should have logs");
        Console.WriteLine("=".PadRight(80, '='));
    }

    [Test]
    [Category("RealTest")]
    public async Task CrawlMangaFullAsync_RealCrawl_ShouldCrawlMangaWithChaptersAndPages()
    {
        // Arrange - Sử dụng URL thật của một manga phổ biến
        var mangaUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece";

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("REAL TEST: CrawlMangaFullAsync - Crawl manga thật với chapters và pages");
        Console.WriteLine($"Manga URL: {mangaUrl}");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawlJobService.CrawlMangaFullAsync(
            _source,
            mangaUrl,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);
        stopwatch.Stop();

        // Assert
        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("TEST RESULTS");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Manga ID: {result.Data}");
        Console.WriteLine($"Time Taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine();

        if (!result.IsSuccess)
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
            Assert.Fail($"Crawl failed: {result.ErrorMessage}");
            return;
        }

        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.EqualTo(Guid.Empty));

        // Verify manga was saved
        var manga = await _unitOfWork.Mangas.GetByIdAsync(result.Data);
        Assert.That(manga, Is.Not.Null, "Manga should be saved to database");
        
        Console.WriteLine($"Manga Title: {manga!.Title}");
        Console.WriteLine($"Manga Source URL: {manga.SourceUrl}");
        Console.WriteLine($"Manga Source ID: {manga.SourceMangaId}");
        Console.WriteLine();

        // Verify chapters were saved
        var chapters = await _unitOfWork.Chapters.FindAsync(c => c.MangaId == manga.Id);
        var chapterList = chapters.ToList();
        
        Console.WriteLine($"Chapters in Database: {chapterList.Count}");
        
        if (chapterList.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Sample Chapters:");
            foreach (var chapter in chapterList)
            {
                Console.WriteLine($"  - {chapter.Title} (Index: {chapter.ChapterIndex})");
                
                // Verify pages for this chapter
                var chapterPages = await _unitOfWork.Pages.FindAsync(p => p.ChapterId == chapter.Id);
                var pageList = chapterPages.ToList();
                Console.WriteLine($"    Pages: {pageList.Count}");
            }

            // Verify at least one chapter has pages
            var firstChapter = chapterList.First();
            var firstChapterPages = await _unitOfWork.Pages.FindAsync(p => p.ChapterId == firstChapter.Id);
            Assert.That(firstChapterPages, Is.Not.Empty, "At least one chapter should have pages");
        }
        else
        {
            Console.WriteLine("Warning: No chapters were crawled");
        }

        // Verify job was created
        var jobs = await _unitOfWork.CrawlJobs.GetAllAsync();
        var job = jobs.FirstOrDefault(j => j.SourceId == _source.Id && j.Type == CrawlJobType.SingleManga);
        
        if (job != null)
        {
            Console.WriteLine();
            Console.WriteLine($"Job Status: {job.Status}");
            Console.WriteLine($"Job Duration: {job.Duration?.TotalSeconds:F2} seconds");
        }

        Console.WriteLine("=".PadRight(80, '='));
    }

    [Test]
    [Category("RealTest")]
    public async Task CrawlMangaChaptersAsync_RealCrawl_ShouldCrawlChapters()
    {
        // Arrange - Tạo một manga trước, sau đó crawl chapters
        var manga = new Manga
        {
            Id = Guid.NewGuid(),
            Title = "One Piece",
            SourceId = _source.Id,
            SourceMangaId = "one-piece",
            SourceUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Mangas.AddAsync(manga);
        await _unitOfWork.SaveChangesAsync();

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("REAL TEST: CrawlMangaChaptersAsync - Crawl chapters thật");
        Console.WriteLine($"Manga: {manga.Title}");
        Console.WriteLine($"Manga URL: {manga.SourceUrl}");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawlJobService.CrawlMangaChaptersAsync(
            _source,
            manga.Id,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);
        stopwatch.Stop();

        // Assert
        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("TEST RESULTS");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Chapters Saved: {result.Data}");
        Console.WriteLine($"Time Taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine();

        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.GreaterThanOrEqualTo(0));

        // Verify chapters were saved
        var chapters = await _unitOfWork.Chapters.FindAsync(c => c.MangaId == manga.Id);
        var chapterList = chapters.ToList();
        
        Console.WriteLine($"Chapters in Database: {chapterList.Count}");
        Assert.That(chapterList, Is.Not.Empty, "Chapters should be saved");

        if (chapterList.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Sample Chapters:");
            foreach (var chapter in chapterList.Take(10))
            {
                Console.WriteLine($"  - {chapter.Title} (Index: {chapter.ChapterIndex}, Number: {chapter.ChapterNumber})");
            }
        }

        Console.WriteLine("=".PadRight(80, '='));
    }

    [Test]
    [Category("RealTest")]
    public async Task CrawlChapterPagesAsync_RealCrawl_ShouldCrawlPages()
    {
        // Arrange - Tạo manga và chapter trước
        var manga = new Manga
        {
            Id = Guid.NewGuid(),
            Title = "One Piece",
            SourceId = _source.Id,
            SourceMangaId = "one-piece",
            SourceUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece",
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
            SourceUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece/chapter-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Chapters.AddAsync(chapter);
        await _unitOfWork.SaveChangesAsync();

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("REAL TEST: CrawlChapterPagesAsync - Crawl pages thật");
        Console.WriteLine($"Chapter: {chapter.Title}");
        Console.WriteLine($"Chapter URL: {chapter.SourceUrl}");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawlJobService.CrawlChapterPagesAsync(
            _source,
            chapter.Id,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);
        stopwatch.Stop();

        // Assert
        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("TEST RESULTS");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Pages Saved: {result.Data}");
        Console.WriteLine($"Time Taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine();

        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.GreaterThanOrEqualTo(0));

        // Verify pages were saved
        var pages = await _unitOfWork.Pages.FindAsync(p => p.ChapterId == chapter.Id);
        var pageList = pages.ToList();
        
        Console.WriteLine($"Pages in Database: {pageList.Count}");
        Assert.That(pageList, Is.Not.Empty, "Pages should be saved");

        if (pageList.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Sample Pages:");
            foreach (var page in pageList)
            {
                Console.WriteLine($"  - Page {page.PageNumber}: {page.ImageUrl}");
            }
        }

        Console.WriteLine("=".PadRight(80, '='));
    }

    [Test]
    [Category("RealTest")]
    public async Task FullFlow_RealCrawl_SearchToMangaToChapterToPage()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("REAL TEST: Full Flow - Search -> Manga -> Chapter -> Page");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Step 1: Crawl search page để lấy danh sách mangas
        Console.WriteLine("Step 1: Crawling search page...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var searchResult = await _crawlJobService.CrawlAllMangasAsync(
            _source,
            crawlJob: null,
            maxPages: 1, // Chỉ 1 trang để test nhanh
            maxConcurrency: 2,
            cancellationToken: default);
        
        stopwatch.Stop();
        Console.WriteLine($"  Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Mangas Found: {searchResult.Data}");
        Console.WriteLine();

        Assert.That(searchResult.IsSuccess, Is.True, "Search should succeed");

        // Step 2: Lấy một manga và crawl đầy đủ
        var mangas = await _unitOfWork.Mangas.FindAsync(m => m.SourceId == _source.Id);
        var mangaList = mangas.ToList();
        
        if (mangaList.Count == 0)
        {
            Assert.Fail("No mangas found from search");
            return;
        }

        var testManga = mangaList.First();
        Console.WriteLine($"Step 2: Crawling full manga: {testManga.Title}");
        Console.WriteLine($"  URL: {testManga.SourceUrl}");
        
        stopwatch.Restart();
        var mangaResult = await _crawlJobService.CrawlMangaFullAsync(
            _source,
            testManga.SourceUrl,
            crawlJob: null,
            skipExisting: false,
            cancellationToken: default);
        stopwatch.Stop();
        
        Console.WriteLine($"  Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Success: {mangaResult.IsSuccess}");
        Console.WriteLine();

        if (mangaResult.IsSuccess)
        {
            // Step 3: Verify data
            var updatedManga = await _unitOfWork.Mangas.GetByIdAsync(mangaResult.Data);
            var chapters = await _unitOfWork.Chapters.FindAsync(c => c.MangaId == mangaResult.Data);
            var chapterList = chapters.ToList();

            Console.WriteLine("Step 3: Verifying data...");
            Console.WriteLine($"  Manga: {updatedManga!.Title}");
            Console.WriteLine($"  Chapters: {chapterList.Count}");

            if (chapterList.Count > 0)
            {
                var firstChapter = chapterList.First();
                var chapterPages = await _unitOfWork.Pages.FindAsync(p => p.ChapterId == firstChapter.Id);
                var pageList = chapterPages.ToList();
                Console.WriteLine($"  Pages in first chapter: {pageList.Count}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Full flow test completed!");
        Console.WriteLine("=".PadRight(80, '='));
    }
}

/// <summary>
/// Real crawler factory - sử dụng NettruyenCrawler thật
/// </summary>
public class RealCrawlerFactory : ICrawlerFactory
{
    private readonly IHtmlParser _htmlParser;

    public RealCrawlerFactory(IHtmlParser htmlParser)
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

