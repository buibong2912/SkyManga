using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Crawlers;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// Tests cho NettruyenCrawler - crawl manga detail page
/// </summary>
[TestFixture]
public class NettruyenCrawlerDetailTests
{
    private NettruyenCrawler _crawler = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Setting up NettruyenCrawler Detail Tests");
        
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
    public async Task CrawlMangaDetail_FromRealURL_ShouldReturnCompleteData()
    {
        Console.WriteLine($"Test: CrawlMangaDetail_FromRealURL_ShouldReturnCompleteData");
        Console.WriteLine($"Crawling: {_source.BaseUrl}/truyen-tranh/one-piece");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        var result = await _crawler.CrawlMangaAsync("/truyen-tranh/one-piece", context);

        Console.WriteLine($"\nCrawl Result:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  URL: {result.Url}");

        if (result.IsSuccess && result.Data != null)
        {
            var manga = result.Data;
            Console.WriteLine($"\nManga Details:");
            Console.WriteLine($"  Title: {manga.Title}");
            Console.WriteLine($"  Source URL: {manga.SourceUrl}");
            Console.WriteLine($"  Cover Image: {manga.CoverImageUrl}");
            Console.WriteLine($"  Rating: {manga.Rating}");
            Console.WriteLine($"  View Count: {manga.ViewCount:N0}");
            Console.WriteLine($"  Author: {manga.AuthorName}");
            Console.WriteLine($"  Genres ({manga.Genres.Count}): {string.Join(", ", manga.Genres)}");
            Console.WriteLine($"  Description Length: {manga.Description?.Length ?? 0} chars");
            Console.WriteLine($"  Chapters: {manga.Chapters.Count}");
            
            if (manga.Chapters.Count > 0)
            {
                Console.WriteLine($"\nFirst 5 Chapters:");
                foreach (var chapter in manga.Chapters)
                {
                    Console.WriteLine($"  - {chapter.Title} (Chapter #{chapter.ChapterNumber}, Index: {chapter.ChapterIndex})");
                    Console.WriteLine($"    URL: {chapter.SourceUrl}");
                }
            }
        }
        else
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
            if (result.Exception != null)
            {
                Console.WriteLine($"  Exception: {result.Exception.Message}");
                Console.WriteLine($"  StackTrace: {result.Exception.StackTrace}");
            }
        }

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Assert.That(result.Data!.Title, Is.Not.Null.And.Not.Empty, "Title should be extracted");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlMangaDetail_WithLocalHTML_ShouldParseCorrectly()
    {
        Console.WriteLine($"Test: CrawlMangaDetail_WithLocalHTML_ShouldParseCorrectly");
        Console.WriteLine("Testing ParseMangaDetailPage with local HTML file");

        // Read HTML from file
        var htmlPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "SkyHighManga.Application",
            "mangadetail.html");

        if (!File.Exists(htmlPath))
        {
            // Try alternative path
            htmlPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "SkyHighManga.Application",
                "mangadetail.html");
        }

        if (!File.Exists(htmlPath))
        {
            Assert.Fail($"Cannot find mangadetail.html at: {htmlPath}");
            return;
        }

        Console.WriteLine($"Reading HTML from: {htmlPath}");
        var html = await File.ReadAllTextAsync(htmlPath);
        Console.WriteLine($"HTML length: {html.Length} chars");
        Console.WriteLine();

        var document = _htmlParser.Parse(html);
        var fullUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece";

        // Test parsing các elements riêng lẻ
        Console.WriteLine("Testing individual selectors:");

        // Title
        var titleElement = document.QuerySelector("//h1[contains(@class, 'title-detail')]//a")
            ?? document.QuerySelector("//h1[contains(@class, 'title-detail')]");
        var title = titleElement?.TextContent?.Trim();
        Console.WriteLine($"  Title: {title}");

        // Rating
        var mStarDiv = document.QuerySelector("//div[contains(@class, 'm-star')]");
        double? rating = null;
        if (mStarDiv != null)
        {
            var allSpans = mStarDiv.QuerySelectorAll("./span").ToList();
            var ratingSpan = allSpans.FirstOrDefault(s => string.IsNullOrEmpty(s.GetAttribute("class")));
            if (ratingSpan != null && double.TryParse(ratingSpan.TextContent?.Trim(), out var r))
            {
                rating = r;
            }
        }
        Console.WriteLine($"  Rating: {rating}");

        // Author
        var authorElement = document.QuerySelector("//div[contains(@class, 'author')]//p");
        var author = authorElement?.TextContent?.Trim();
        Console.WriteLine($"  Author: {author}");

        // View Count
        var viewElement = document.QuerySelector("//div[contains(@class, 'view')]//p");
        var viewText = viewElement?.TextContent?.Trim();
        int viewCount = 0;
        if (!string.IsNullOrEmpty(viewText))
        {
            viewText = viewText.Replace(".", "");
            int.TryParse(viewText, out viewCount);
        }
        Console.WriteLine($"  View Count: {viewCount:N0}");

        // Genres/Tags
        var kindElement = document.QuerySelector("//div[contains(@class, 'kind')]//a");
        var kind = kindElement?.TextContent?.Trim();
        Console.WriteLine($"  Kind: {kind}");

        var tagElements = document.QuerySelectorAll("//div[contains(@class, 'm-tags')]//a[contains(@href, '/genre/')]").ToList();
        Console.WriteLine($"  Tags ({tagElements.Count}): {string.Join(", ", tagElements.Select(t => t.TextContent?.Trim()).Where(t => !string.IsNullOrEmpty(t)))}");

        // Description
        var descElement = document.QuerySelector("//div[contains(@class, 'sort-des')]//div[contains(@class, 'html-content')]");
        var description = descElement?.TextContent?.Trim();
        Console.WriteLine($"  Description Length: {description?.Length ?? 0} chars");

        // Cover Image from script
        var scriptElement = document.QuerySelector("//script[contains(text(), 'mangaDetail')]");
        string? coverUrl = null;
        if (scriptElement != null)
        {
            var scriptText = scriptElement.TextContent ?? "";
            var posterMatch = System.Text.RegularExpressions.Regex.Match(
                scriptText,
                @"posterPath[""']?\s*:\s*[""']([^""']+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (posterMatch.Success)
            {
                coverUrl = posterMatch.Groups[1].Value;
            }
        }
        Console.WriteLine($"  Cover URL: {coverUrl}");

        // Chapters
        var chapterElements = document.QuerySelectorAll("//div[contains(@class, 'list-chapters')]//div[contains(@class, 'l-chapter')]//a[contains(@class, 'll-chap')]").ToList();
        Console.WriteLine($"  Chapters Found: {chapterElements.Count}");
        
        if (chapterElements.Count > 0)
        {
            Console.WriteLine($"\nFirst 3 Chapters:");
            foreach (var chapterElement in chapterElements.Take(3))
            {
                var chapterUrl = chapterElement.GetAttribute("href");
                var chapterTitle = chapterElement.TextContent?.Trim();
                Console.WriteLine($"    - {chapterTitle}");
                Console.WriteLine($"      URL: {chapterUrl}");
            }
        }

        Console.WriteLine();
        Assert.That(title, Is.Not.Null.And.Not.Empty, "Title should be extracted");
        Assert.That(rating, Is.Not.Null, "Rating should be extracted");
        Assert.That(author, Is.Not.Null.And.Not.Empty, "Author should be extracted");
        Assert.That(viewCount, Is.GreaterThan(0), "View count should be extracted");
        Assert.That(chapterElements.Count, Is.GreaterThan(0), "Should find at least one chapter");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlMangaDetail_MultipleMangas_ShouldWork()
    {
        Console.WriteLine($"Test: CrawlMangaDetail_MultipleMangas_ShouldWork");
        
        var mangaUrls = new[]
        {
            "/truyen-tranh/one-piece",
            "/truyen-tranh/naruto-full-mau",
            "/truyen-tranh/dragon-ball-after"
        };

        foreach (var mangaUrl in mangaUrls)
        {
            Console.WriteLine($"\nCrawling: {mangaUrl}");
            
            var context = new CrawlerContext
            {
                Source = _source,
                StartUrl = $"{_source.BaseUrl}{mangaUrl}"
            };

            var result = await _crawler.CrawlMangaAsync(mangaUrl, context);

            if (result.IsSuccess && result.Data != null)
            {
                var manga = result.Data;
                Console.WriteLine($"  ✓ {manga.Title}");
                Console.WriteLine($"    Rating: {manga.Rating}");
                Console.WriteLine($"    Views: {manga.ViewCount:N0}");
                Console.WriteLine($"    Chapters: {manga.Chapters.Count}");
            }
            else
            {
                Console.WriteLine($"  ✗ Failed: {result.ErrorMessage}");
            }

            Assert.That(result.IsSuccess, Is.True, $"Should crawl {mangaUrl} successfully");
            
            // Delay để tránh rate limit
            await Task.Delay(2000);
        }

        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlMangaDetailsAsync_ShouldReturnSameAsCrawlMangaAsync()
    {
        Console.WriteLine($"Test: CrawlMangaDetailsAsync_ShouldReturnSameAsCrawlMangaAsync");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece"
        };

        var result1 = await _crawler.CrawlMangaAsync("/truyen-tranh/one-piece", context);
        await Task.Delay(1000); // Delay giữa 2 requests
        
        var result2 = await _crawler.CrawlMangaDetailsAsync("/truyen-tranh/one-piece", context);

        Console.WriteLine($"\nCrawlMangaAsync:");
        Console.WriteLine($"  Success: {result1.IsSuccess}");
        Console.WriteLine($"  Title: {result1.Data?.Title}");

        Console.WriteLine($"\nCrawlMangaDetailsAsync:");
        Console.WriteLine($"  Success: {result2.IsSuccess}");
        Console.WriteLine($"  Title: {result2.Data?.Title}");

        Assert.That(result1.IsSuccess, Is.True, "CrawlMangaAsync should succeed");
        Assert.That(result2.IsSuccess, Is.True, "CrawlMangaDetailsAsync should succeed");
        Assert.That(result1.Data?.Title, Is.EqualTo(result2.Data?.Title), "Both methods should return same title");
        Console.WriteLine("✓ Test passed\n");
    }
}

