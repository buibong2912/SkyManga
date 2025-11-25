using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Crawlers;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// Tests cho full crawl toàn bộ dữ liệu từ search (14k+ items)
/// </summary>
[TestFixture]
public class NettruyenCrawlerFullCrawlTests
{
    private NettruyenCrawler _crawler = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Setting up NettruyenCrawler Full Crawl Tests");
        
        _htmlParser = new HtmlAgilityPackParser();
        _crawler = new NettruyenCrawler(_htmlParser);
        
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

        Console.WriteLine($"Source: {_source.Name} - {_source.BaseUrl}");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
    }

    [TearDown]
    public void TearDown()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
    }

    [Test]
    [Category("FullCrawl")]
    public async Task FullCrawl_AllSearchResults_ShouldCrawlAllPages()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("FULL CRAWL TEST: Crawl toàn bộ dữ liệu từ search (14k+ items)");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
        Console.WriteLine("⚠ WARNING: Test này sẽ crawl TẤT CẢ các trang (có thể > 1400 trang)");
        Console.WriteLine("⚠ Thời gian ước tính: 30-60 phút tùy vào tốc độ mạng và rate limiting");
        Console.WriteLine("⚠ Sẽ tạo ra > 14,000 manga items");
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) =>
            {
                // Chỉ log Info và Warning để tránh spam
                if (level == Application.Common.Models.LogLevel.Info || 
                    level == Application.Common.Models.LogLevel.Warning ||
                    level == Application.Common.Models.LogLevel.Error)
                {
                    Console.WriteLine($"  [{level}] {msg}");
                }
            },
            OnProgress = (processed, total) =>
            {
                // Log progress mỗi 100 items hoặc mỗi 10%
                if (processed % 100 == 0 || (total > 0 && processed % (total / 10) == 0))
                {
                    var percent = total > 0 ? (processed * 100.0 / total) : 0;
                    Console.WriteLine($"  Progress: {processed}/{total} ({percent:F1}%)");
                }
            }
        };

        var keyword = ""; // Hoặc keyword khác để có nhiều kết quả
        var maxPages=10; // 0 = crawl tất cả các trang

        Console.WriteLine($"Search Keyword: '{keyword}'");
        Console.WriteLine($"Max Pages: {maxPages} (all pages)");
        Console.WriteLine($"Max Results: null (no limit)");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var startTime = DateTime.Now;

        try
        {
            var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: maxPages);

            stopwatch.Stop();
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            Assert.That(result.IsSuccess, Is.True, "Full crawl should succeed");
            Assert.That(result.Data, Is.Not.Null, "Search data should not be null");
            Assert.That(result.SuccessCount, Is.GreaterThan(1000), "Should crawl at least 1000 items");

            var mangas = result.Data!.ToList();

            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("FULL CRAWL SUMMARY");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"✓ Total Items Crawled: {mangas.Count:N0}");
            Console.WriteLine($"✓ Total Time: {duration.TotalMinutes:F2} minutes ({duration.TotalSeconds:F0} seconds)");
            Console.WriteLine($"✓ Average Speed: {mangas.Count / duration.TotalMinutes:F1} items/minute");
            Console.WriteLine($"✓ Average Speed: {mangas.Count / duration.TotalSeconds:F2} items/second");
            Console.WriteLine();

            // Verify không có duplicate
            var uniqueUrls = mangas.Select(m => m.SourceUrl).Distinct().ToList();
            Console.WriteLine($"✓ Unique URLs: {uniqueUrls.Count:N0}");
            Console.WriteLine($"✓ Duplicates Found: {mangas.Count - uniqueUrls.Count:N0}");
            Assert.That(uniqueUrls.Count, Is.EqualTo(mangas.Count), "Should not have duplicate URLs");

            // Statistics
            var withRating = mangas.Count(m => m.Rating.HasValue);
            var withViews = mangas.Sum(m => m.ViewCount);
            var withChapters = mangas.Count(m => m.Chapters != null && m.Chapters.Count > 0);

            Console.WriteLine();
            Console.WriteLine("Statistics:");
            Console.WriteLine($"  - Items with Rating: {withRating:N0} ({withRating * 100.0 / mangas.Count:F1}%)");
            Console.WriteLine($"  - Items with View Count: {withViews:N0} ({withViews * 100.0 / mangas.Count:F1}%)");
            Console.WriteLine($"  - Items with Chapters: {withChapters:N0} ({withChapters * 100.0 / mangas.Count:F1}%)");
            Console.WriteLine();

            // Sample results
            Console.WriteLine("Sample Results (first 10):");
            foreach (var manga in mangas)
            {
                Console.WriteLine($"  - {manga.Title}");
                Console.WriteLine($"    URL: {manga.SourceUrl}");
                Console.WriteLine($"    Rating: {manga.Rating?.ToString("F1") ?? "N/A"}");
                Console.WriteLine($"    Views: {manga.ViewCount.ToString("N0") ?? "N/A"}");
            }
            Console.WriteLine();

            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("✓ Full crawl completed successfully!");
            Console.WriteLine("=".PadRight(80, '='));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"✗ Full crawl failed after {stopwatch.Elapsed.TotalMinutes:F2} minutes");
            Console.WriteLine($"  Error: {ex.Message}");
            throw;
        }
    }

    [Test]
    [Category("FullCrawl")]
    public async Task FullCrawl_WithCancellation_ShouldStopGracefully()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Full Crawl with Cancellation");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var cts = new CancellationTokenSource();
        
        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            CancellationToken = cts.Token,
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}")
        };

        var keyword = "One Piece";
        var maxPages = 0; // All pages

        Console.WriteLine($"Search Keyword: '{keyword}'");
        Console.WriteLine($"Will cancel after 5 seconds...");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        // Cancel after 5 seconds
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: maxPages);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"Crawl stopped after: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Items crawled before cancellation: {result.Data.Count() }");
        Console.WriteLine();

        // Should have some results before cancellation
        if (result.IsSuccess && result.Data != null)
        {
            Assert.That(result.Data.Count, Is.GreaterThan(0), "Should have crawled some items before cancellation");
        }
    }

    [Test]
    [Category("FullCrawl")]
    public async Task FullCrawl_WithMaxResults_ShouldRespectLimit()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Full Crawl with MaxResults Limit");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) =>
            {
                if (level == Application.Common.Models.LogLevel.Info || 
                    level == Application.Common.Models.LogLevel.Warning)
                    Console.WriteLine($"  [{level}] {msg}");
            },
            OnProgress = (processed, total) =>
            {
                if (processed % 50 == 0)
                {
                    var percent = total > 0 ? (processed * 100.0 / total) : 0;
                    Console.WriteLine($"  Progress: {processed}/{total} ({percent:F1}%)");
                }
            }
        };

        var keyword = "One Piece";
        var maxResults = 500; // Giới hạn 500 items
        var maxPages = 0; // Nhưng crawl tất cả các trang (sẽ dừng khi đạt maxResults)

        Console.WriteLine($"Search Keyword: '{keyword}'");
        Console.WriteLine($"Max Results: {maxResults}");
        Console.WriteLine($"Max Pages: {maxPages} (all pages, but will stop at {maxResults} items)");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: maxResults, maxPages: maxPages);
        stopwatch.Stop();

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Assert.That(result.SuccessCount, Is.LessThanOrEqualTo(maxResults), 
            $"Should not exceed {maxResults} results");

        var mangas = result.Data!.ToList();

        Console.WriteLine();
        Console.WriteLine($"✓ Total Items: {mangas.Count:N0}");
        Console.WriteLine($"✓ Time Taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"✓ Within Limit: {mangas.Count <= maxResults}");
        Console.WriteLine();
    }

    [Test]
    [Category("FullCrawl")]
    public async Task FullCrawl_PerformanceTest_MeasureSpeed()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Full Crawl Performance - Measure Speed");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        var keyword = "One Piece";
        var maxPages = 10; // Test với 10 trang để đo performance

        Console.WriteLine($"Search Keyword: '{keyword}'");
        Console.WriteLine($"Max Pages: {maxPages} (for performance testing)");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: maxPages);
        stopwatch.Stop();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);

        var mangas = result.Data!.ToList();
        var pagesCrawled = maxPages;
        var itemsPerPage = mangas.Count / (double)pagesCrawled;
        var timePerPage = stopwatch.Elapsed.TotalSeconds / pagesCrawled;
        var itemsPerSecond = mangas.Count / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine();
        Console.WriteLine("Performance Metrics:");
        Console.WriteLine($"  Total Items: {mangas.Count:N0}");
        Console.WriteLine($"  Pages Crawled: {pagesCrawled}");
        Console.WriteLine($"  Total Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"  Items per Page: {itemsPerPage:F1}");
        Console.WriteLine($"  Time per Page: {timePerPage:F2} seconds");
        Console.WriteLine($"  Items per Second: {itemsPerSecond:F2}");
        Console.WriteLine($"  Estimated Time for 1400 pages: {(timePerPage * 1400 / 60):F1} minutes");
        Console.WriteLine();
    }
}


