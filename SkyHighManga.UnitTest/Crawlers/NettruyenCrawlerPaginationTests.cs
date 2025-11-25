using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Crawlers;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// Tests cho pagination trong search results
/// </summary>
[TestFixture]
public class NettruyenCrawlerPaginationTests
{
    private NettruyenCrawler _crawler = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Setting up NettruyenCrawler Pagination Tests");
        
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
    public async Task SearchMangaAsync_WithPagination_ShouldCrawlMultiplePages()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Search with Pagination - Crawl Multiple Pages");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        var keyword = "";
        var maxPages = 10; // Crawl 3 trang đầu tiên

        Console.WriteLine($"Searching for: '{keyword}'");
        Console.WriteLine($"Max Pages: {maxPages}");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: maxPages);

        Assert.That(result.IsSuccess, Is.True, "Search should succeed");
        Assert.That(result.Data, Is.Not.Null, "Search data should not be null");
        Assert.That(result.SuccessCount, Is.GreaterThan(0), "Should find at least one manga");

        var mangas = result.Data!.ToList();
        Console.WriteLine($"✓ Total results: {mangas.Count}");
        Console.WriteLine($"✓ Results from {maxPages} pages");
        Console.WriteLine();

        // Verify không có duplicate
        var uniqueUrls = mangas.Select(m => m.SourceUrl).Distinct().ToList();
        Console.WriteLine($"✓ Unique URLs: {uniqueUrls.Count}");
        Console.WriteLine($"✓ Duplicates: {mangas.Count - uniqueUrls.Count}");
        
        Assert.That(uniqueUrls.Count, Is.EqualTo(mangas.Count), "Should not have duplicate URLs");

        // Hiển thị một số kết quả
        Console.WriteLine("\nFirst 10 results:");
        foreach (var manga in mangas.Take(10))
        {
            Console.WriteLine($"  - {manga.Title} ({manga.SourceUrl})");
        }
        Console.WriteLine();
    }

    [Test]
    public async Task SearchMangaAsync_WithMaxResults_ShouldRespectLimit()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Search with MaxResults - Should Respect Limit");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        var keyword = "One Piece";
        var maxResults = 15; // Chỉ lấy 15 kết quả
        var maxPages = 3; // Nhưng crawl 3 trang

        Console.WriteLine($"Searching for: '{keyword}'");
        Console.WriteLine($"Max Results: {maxResults}");
        Console.WriteLine($"Max Pages: {maxPages}");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: maxResults, maxPages: maxPages);

        Assert.That(result.IsSuccess, Is.True, "Search should succeed");
        Assert.That(result.Data, Is.Not.Null, "Search data should not be null");
        Assert.That(result.SuccessCount, Is.LessThanOrEqualTo(maxResults), 
            $"Should not exceed {maxResults} results");

        var mangas = result.Data!.ToList();
        Console.WriteLine($"✓ Total results: {mangas.Count}");
        Console.WriteLine($"✓ Limit: {maxResults}");
        Console.WriteLine($"✓ Within limit: {mangas.Count <= maxResults}");
        Console.WriteLine();
    }

    [Test]
    public async Task SearchMangaAsync_DefaultBehavior_ShouldCrawlOnlyFirstPage()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Search Default Behavior - Should Crawl Only First Page");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        var keyword = "One Piece";

        Console.WriteLine($"Searching for: '{keyword}'");
        Console.WriteLine($"Max Pages: null (default - first page only)");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: null);

        Assert.That(result.IsSuccess, Is.True, "Search should succeed");
        Assert.That(result.Data, Is.Not.Null, "Search data should not be null");

        var mangas = result.Data!.ToList();
        Console.WriteLine($"✓ Total results: {mangas.Count}");
        Console.WriteLine($"✓ Should be from first page only (typically 10-20 items per page)");
        Console.WriteLine();

        // Thường mỗi trang có khoảng 10-20 items
        Assert.That(mangas.Count, Is.LessThan(50), 
            "Default behavior should only crawl first page (typically < 50 items)");
    }

    [Test]
    public async Task SearchMangaAsync_WithMaxPagesZero_ShouldCrawlAllPages()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Search with MaxPages=0 - Should Crawl All Pages (Limited Test)");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) => 
            {
                if (level == Application.Common.Models.LogLevel.Info || level == Application.Common.Models.LogLevel.Warning)
                    Console.WriteLine($"  [{level}] {msg}");
            }
        };

        var keyword = "One Piece";
        var maxResults = 50; // Giới hạn để test không quá lâu
        var maxPages = 0; // 0 = crawl tất cả các trang

        Console.WriteLine($"Searching for: '{keyword}'");
        Console.WriteLine($"Max Pages: 0 (all pages, but limited by maxResults={maxResults})");
        Console.WriteLine("⚠ Note: This test may take a while if there are many pages");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: maxResults, maxPages: maxPages);
        stopwatch.Stop();

        Assert.That(result.IsSuccess, Is.True, "Search should succeed");
        Assert.That(result.Data, Is.Not.Null, "Search data should not be null");

        var mangas = result.Data!.ToList();
        Console.WriteLine($"✓ Total results: {mangas.Count}");
        Console.WriteLine($"✓ Time taken: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / 1000.0:F2}s)");
        Console.WriteLine($"✓ Results limited by maxResults: {mangas.Count <= maxResults}");
        Console.WriteLine();
    }

    [Test]
    public async Task SearchMangaAsync_Pagination_ShouldParseTotalPages()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Pagination - Should Parse Total Pages");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}")
        };

        var keyword = "One Piece";

        Console.WriteLine($"Searching for: '{keyword}'");
        Console.WriteLine("Checking if pagination info is parsed correctly...");
        Console.WriteLine("-".PadRight(80, '-'));
        Console.WriteLine();

        // Crawl với maxPages = 1 để xem log về tổng số trang
        var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: 5, maxPages: 1);

        Assert.That(result.IsSuccess, Is.True, "Search should succeed");
        Assert.That(result.Data, Is.Not.Null, "Search data should not be null");

        var mangas = result.Data!.ToList();
        Console.WriteLine($"✓ Results from first page: {mangas.Count}");
        Console.WriteLine("✓ Check console logs above for total pages information");
        Console.WriteLine();
    }

    [Test]
    public async Task SearchMangaAsync_CompareSinglePageVsMultiplePages()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: Compare Single Page vs Multiple Pages");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        var keyword = "One Piece";

        // Test 1: Chỉ trang đầu tiên
        Console.WriteLine("Test 1: Crawl only first page");
        var result1 = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: 1);
        Assert.That(result1.IsSuccess, Is.True);
        var mangas1 = result1.Data!.ToList();
        Console.WriteLine($"  Results: {mangas1.Count}");
        Console.WriteLine();

        await Task.Delay(2000);

        // Test 2: Crawl 3 trang
        Console.WriteLine("Test 2: Crawl 3 pages");
        var result2 = await _crawler.SearchMangaAsync(keyword, context, maxResults: null, maxPages: 3);
        Assert.That(result2.IsSuccess, Is.True);
        var mangas2 = result2.Data!.ToList();
        Console.WriteLine($"  Results: {mangas2.Count}");
        Console.WriteLine();

        // Verify
        Console.WriteLine("Comparison:");
        Console.WriteLine($"  Single page: {mangas1.Count} results");
        Console.WriteLine($"  3 pages: {mangas2.Count} results");
        Console.WriteLine($"  Difference: {mangas2.Count - mangas1.Count} additional results");
        
        // Kết quả từ 3 trang nên nhiều hơn hoặc bằng kết quả từ 1 trang
        Assert.That(mangas2.Count, Is.GreaterThanOrEqualTo(mangas1.Count), 
            "Results from 3 pages should be >= results from 1 page");

        // Tất cả kết quả từ trang 1 nên có trong kết quả từ 3 trang
        var urls1 = mangas1.Select(m => m.SourceUrl).ToHashSet();
        var urls2 = mangas2.Select(m => m.SourceUrl).ToHashSet();
        var allIncluded = urls1.IsSubsetOf(urls2);
        
        Console.WriteLine($"  All page 1 results in 3-page results: {allIncluded}");
        Assert.That(allIncluded, Is.True, "All results from page 1 should be in 3-page results");
        Console.WriteLine();
    }
}


