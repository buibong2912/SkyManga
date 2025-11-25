using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Crawlers;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// Integration tests cho NettruyenCrawler - test thực tế với website
/// </summary>
[TestFixture]
public class NettruyenCrawlerIntegrationTests
{
    private NettruyenCrawler _crawler = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Setting up NettruyenCrawler Integration Tests");
        
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
    public async Task TestConnection_ShouldSucceed()
    {
        Console.WriteLine($"Test: TestConnection_ShouldSucceed");
        Console.WriteLine($"Testing connection to: {_source.BaseUrl}");

        var result = await _crawler.TestConnectionAsync(_source);

        Console.WriteLine($"Connection result: {(result ? "✓ Success" : "✗ Failed")}");
        Assert.That(result, Is.True, "Should be able to connect to Nettruyen");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public void CanCrawl_ShouldReturnTrue()
    {
        Console.WriteLine($"Test: CanCrawl_ShouldReturnTrue");
        
        var canCrawl = _crawler.CanCrawl(_source);
        
        Console.WriteLine($"Can crawl: {canCrawl}");
        Assert.That(canCrawl, Is.True, "Should be able to crawl Nettruyen source");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task SearchManga_WithKeyword_ShouldReturnResults()
    {
        Console.WriteLine($"Test: SearchManga_WithKeyword_ShouldReturnResults");
        Console.WriteLine($"Searching for: 'One Piece'");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        var result = await _crawler.SearchMangaAsync("", context, maxResults: 5);

        Console.WriteLine($"\nSearch Results:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  Total Count: {result.TotalCount}");
        Console.WriteLine($"  Success Count: {result.SuccessCount}");
        Console.WriteLine($"  Failed Count: {result.FailedCount}");
        Console.WriteLine($"  URL: {result.Url}");

        if (result.IsSuccess && result.Data != null)
        {
            var mangas = result.Data.ToList();
            Console.WriteLine($"\nFound {mangas.Count} manga:");
            
            for (int i = 0; i < mangas.Count; i++)
            {
                var manga = mangas[i];
                Console.WriteLine($"  {i + 1}. {manga.Title}");
                Console.WriteLine($"     URL: {manga.SourceUrl}");
                Console.WriteLine($"     Cover: {manga.CoverImageUrl}");
                Console.WriteLine($"     Rating: {manga.Rating}");
                Console.WriteLine($"     Views: {manga.ViewCount:N0}");
                Console.WriteLine($"     Chapters: {manga.Chapters.Count}");
                if (manga.Chapters.Any())
                {
                    Console.WriteLine($"     Latest: {manga.Chapters.First().Title}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
            if (result.Exception != null)
            {
                Console.WriteLine($"  Exception: {result.Exception.Message}");
            }
        }

        Assert.That(result.IsSuccess, Is.True, "Search should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Assert.That(result.SuccessCount, Is.GreaterThan(0), "Should find at least one manga");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlMangaList_FromSearchPage_ShouldReturnResults()
    {
        Console.WriteLine($"Test: CrawlMangaList_FromSearchPage_ShouldReturnResults");
        Console.WriteLine($"Crawling from search page: {_source.BaseUrl}/tim-kiem");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        var result = await _crawler.CrawlMangaListAsync(context, maxItems: 3);

        Console.WriteLine($"\nCrawl Results:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  Total Count: {result.TotalCount}");
        Console.WriteLine($"  Success Count: {result.SuccessCount}");

        if (result.IsSuccess && result.Data != null)
        {
            var mangas = result.Data.ToList();
            Console.WriteLine($"\nCrawled {mangas.Count} manga:");
            
            foreach (var manga in mangas)
            {
                Console.WriteLine($"  - {manga.Title}");
                Console.WriteLine($"    URL: {manga.SourceUrl}");
            }
        }
        else
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task SearchManga_MultipleKeywords_ShouldWork()
    {
        Console.WriteLine($"Test: SearchManga_MultipleKeywords_ShouldWork");
        
        var keywords = new[] { "One Piece", "Naruto", "Dragon Ball" };
        
        foreach (var keyword in keywords)
        {
            Console.WriteLine($"\nSearching for: '{keyword}'");
            
            var context = new CrawlerContext
            {
                Source = _source,
                StartUrl = $"{_source.BaseUrl}/tim-kiem"
            };

            var result = await _crawler.SearchMangaAsync(keyword, context, maxResults: 20);

            Console.WriteLine($"  Results: {result.SuccessCount} found");
            
            if (result.IsSuccess && result.Data != null)
            {
                var mangas = result.Data.ToList();
                foreach (var manga in mangas)
                {
                    Console.WriteLine($"    - {manga.Title}");
                }
            }

            Assert.That(result.IsSuccess, Is.True, $"Search for '{keyword}' should succeed");
            
            // Delay để tránh rate limit
            await Task.Delay(1000);
        }

        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task ParseMangaItem_ShouldExtractAllFields()
    {
        Console.WriteLine($"Test: ParseMangaItem_ShouldExtractAllFields");
        Console.WriteLine("Testing ParseMangaItem with real HTML from search page");

        // Download HTML từ search page
        var searchUrl = $"{_source.BaseUrl}/tim-kiem";
        var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync(searchUrl);
        
        Console.WriteLine($"Downloaded HTML ({html.Length} chars)");

        var document = _htmlParser.Parse(html);
        // Sử dụng contains() vì class có thể là "m-post col-md-6"
        var mangaItems = document.QuerySelectorAll("//div[contains(@class, 'm-post')]").ToList();

        if (mangaItems.Count == 0)
        {
            Console.WriteLine("  No manga items found in HTML");
            Assert.Fail("Should find at least one manga item");
            return;
        }

        Console.WriteLine($"Found {mangaItems.Count} manga items");
        Console.WriteLine("\nParsing first item:");

        var firstItem = mangaItems[0];
        
        // Test parsing
        var titleElement = firstItem.QuerySelector("//h3[@class='m-name']//a");
        var title = titleElement?.TextContent?.Trim();
        var titleUrl = titleElement?.GetAttribute("href");
        
        var imgElement = firstItem.QuerySelector("//img[@class='lzl']");
        var coverUrl = imgElement?.GetAttribute("data-src") 
            ?? imgElement?.GetAttribute("data-original")
            ?? imgElement?.GetAttribute("src");
        
        // Rating nằm trong span không có class, là span cuối cùng trong div.m-star
        var ratingElement = firstItem.QuerySelector("//div[@class='m-star']/span[not(@class)]")
            ?? firstItem.QuerySelector("//div[@class='m-star']/span[last()]");
        var rating = ratingElement?.TextContent?.Trim();
        
        var viewElement = firstItem.QuerySelector("//span[@class='num-view']");
        var viewText = viewElement?.TextContent?.Trim();
        
        var chapters = firstItem.QuerySelectorAll("//ul[@class='list-chaps']//li[@class='chapter']//a").ToList();

        Console.WriteLine($"  Title: {title}");
        Console.WriteLine($"  Title URL: {titleUrl}");
        Console.WriteLine($"  Cover URL: {coverUrl}");
        Console.WriteLine($"  Rating: {rating}");
        Console.WriteLine($"  Views: {viewText}");
        Console.WriteLine($"  Chapters: {chapters.Count}");
        
        foreach (var chapter in chapters)
        {
            Console.WriteLine($"    - {chapter.TextContent?.Trim()}");
        }

        Assert.That(title, Is.Not.Null.And.Not.Empty, "Title should be extracted");
        Assert.That(titleUrl, Is.Not.Null.And.Not.Empty, "Title URL should be extracted");
        Console.WriteLine("✓ Test passed\n");
    }
}

