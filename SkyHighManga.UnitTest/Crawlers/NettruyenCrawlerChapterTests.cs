using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infrastructure.Crawlers;
using SkyHighManga.Infrastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// Tests cho NettruyenCrawler - crawl chapters và pages
/// </summary>
[TestFixture]
public class NettruyenCrawlerChapterTests
{
    private NettruyenCrawler _crawler = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Setting up NettruyenCrawler Chapter Tests");
        
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
    public async Task CrawlChapter_FromRealURL_ShouldReturnChapterWithImages()
    {
        Console.WriteLine($"Test: CrawlChapter_FromRealURL_ShouldReturnChapterWithImages");
        Console.WriteLine($"Crawling: {_source.BaseUrl}/truyen-tranh/one-piece/chapter-1165");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece/chapter-1165",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        var result = await _crawler.CrawlChapterAsync("/truyen-tranh/one-piece/chapter-1165", context);

        Console.WriteLine($"\nCrawl Result:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  URL: {result.Url}");

        if (result.IsSuccess && result.Data != null)
        {
            var chapter = result.Data;
            Console.WriteLine($"\nChapter Details:");
            Console.WriteLine($"  Title: {chapter.Title}");
            Console.WriteLine($"  Chapter Number: {chapter.ChapterNumber}");
            Console.WriteLine($"  Chapter Index: {chapter.ChapterIndex}");
            Console.WriteLine($"  Source Chapter ID: {chapter.SourceChapterId}");
            Console.WriteLine($"  Source URL: {chapter.SourceUrl}");
            Console.WriteLine($"  Page URLs Count: {chapter.PageUrls.Count}");
            
            if (chapter.PageUrls.Count > 0)
            {
                Console.WriteLine($"\nFirst 5 Page URLs:");
                foreach (var pageUrl in chapter.PageUrls.Take(5))
                {
                    Console.WriteLine($"  - {pageUrl}");
                }
                
                Console.WriteLine($"\nLast 3 Page URLs:");
                foreach (var pageUrl in chapter.PageUrls.TakeLast(3))
                {
                    Console.WriteLine($"  - {pageUrl}");
                }
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

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Assert.That(result.Data!.Title, Is.Not.Null.And.Not.Empty, "Title should be extracted");
        Assert.That(result.Data.PageUrls.Count, Is.GreaterThan(0), "Should have at least one page URL");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlChapter_WithLocalHTML_ShouldParseCorrectly()
    {
        Console.WriteLine($"Test: CrawlChapter_WithLocalHTML_ShouldParseCorrectly");
        Console.WriteLine("Testing ParseChapterPage with local HTML file");

        // Read HTML from file
        var htmlPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "SkyHighManga.Application",
            "chapterdetail.html");

        if (!File.Exists(htmlPath))
        {
            // Try alternative path
            htmlPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "SkyHighManga.Application",
                "chapterdetail.html");
        }

        if (!File.Exists(htmlPath))
        {
            Assert.Fail($"Cannot find chapterdetail.html at: {htmlPath}");
            return;
        }

        Console.WriteLine($"Reading HTML from: {htmlPath}");
        var html = await File.ReadAllTextAsync(htmlPath);
        Console.WriteLine($"HTML length: {html.Length} chars");
        Console.WriteLine();

        var document = _htmlParser.Parse(html);
        var fullUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece/chapter-1165";

        // Test parsing script tag
        Console.WriteLine("Testing script tag parsing:");
        var scriptElement = document.QuerySelector("//script[contains(text(), 'chapterDetail')]");
        if (scriptElement != null)
        {
            var scriptText = scriptElement.TextContent ?? "";
            Console.WriteLine($"  Script found: {scriptText.Length} chars");
            
            // Parse chapterDetail JSON
            var nameMatch = System.Text.RegularExpressions.Regex.Match(
                scriptText,
                @"""name""\s*:\s*""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var name = nameMatch.Success ? nameMatch.Groups[1].Value : null;
            Console.WriteLine($"  Chapter Name: {name}");

            var indexMatch = System.Text.RegularExpressions.Regex.Match(
                scriptText,
                @"""index""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var index = indexMatch.Success ? indexMatch.Groups[1].Value : null;
            Console.WriteLine($"  Chapter Index: {index}");

            var idMatch = System.Text.RegularExpressions.Regex.Match(
                scriptText,
                @"""id""\s*:\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var id = idMatch.Success ? idMatch.Groups[1].Value : null;
            Console.WriteLine($"  Chapter ID: {id}");

            // Parse images
            var imagesMatch = System.Text.RegularExpressions.Regex.Match(
                scriptText,
                @"""images""\s*:\s*\[(.*?)\]",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (imagesMatch.Success)
            {
                var imagesJson = imagesMatch.Groups[1].Value;
                var fullImageMatches = System.Text.RegularExpressions.Regex.Matches(
                    imagesJson,
                    @"""index""\s*:\s*(\d+).*?""path""\s*:\s*""([^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                Console.WriteLine($"  Images found: {fullImageMatches.Count}");
                
                var imageList = new List<(int index, string path)>();
                foreach (System.Text.RegularExpressions.Match match in fullImageMatches)
                {
                    if (int.TryParse(match.Groups[1].Value, out var imgIndex))
                    {
                        imageList.Add((imgIndex, match.Groups[2].Value));
                    }
                }

                var sortedImages = imageList.OrderBy(x => x.index).ToList();
                Console.WriteLine($"  Sorted images: {sortedImages.Count}");
                
                if (sortedImages.Count > 0)
                {
                    Console.WriteLine($"  First image (index {sortedImages[0].index}): {sortedImages[0].path}");
                    Console.WriteLine($"  Last image (index {sortedImages[^1].index}): {sortedImages[^1].path}");
                }
            }
        }
        else
        {
            Console.WriteLine("  Script tag not found");
        }

        // Test parsing HTML img elements
        Console.WriteLine("\nTesting HTML img elements:");
        var imgElements = document.QuerySelectorAll("//div[@id='read-chaps']//img[contains(@class, 'reading-img')]").ToList();
        Console.WriteLine($"  Found {imgElements.Count} img elements");
        
        if (imgElements.Count > 0)
        {
            var imageList = new List<(int index, string url)>();
            foreach (var imgElement in imgElements.Take(5))
            {
                var indexAttr = imgElement.GetAttribute("data-indexr");
                var imageUrl = imgElement.GetAttribute("src")
                    ?? imgElement.GetAttribute("data-src")
                    ?? imgElement.GetAttribute("data-original");
                
                Console.WriteLine($"    Index: {indexAttr}, URL: {imageUrl}");
                
                if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.Contains("/images/pre-load"))
                {
                    if (int.TryParse(indexAttr, out var pageIndex))
                    {
                        imageList.Add((pageIndex, imageUrl));
                    }
                }
            }
        }

        Console.WriteLine();
        Assert.That(scriptElement, Is.Not.Null, "Script tag should be found");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlPageUrls_FromChapter_ShouldReturnImageUrls()
    {
        Console.WriteLine($"Test: CrawlPageUrls_FromChapter_ShouldReturnImageUrls");
        Console.WriteLine($"Crawling page URLs from: /truyen-tranh/one-piece/chapter-1165");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece/chapter-1165",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}")
        };

        var result = await _crawler.CrawlPageUrlsAsync("/truyen-tranh/one-piece/chapter-1165", context);

        Console.WriteLine($"\nResult:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  Total Count: {result.TotalCount}");
        Console.WriteLine($"  Success Count: {result.SuccessCount}");

        if (result.IsSuccess && result.Data != null)
        {
            var pageUrls = result.Data.ToList();
            Console.WriteLine($"\nFound {pageUrls.Count} page URLs:");
            
            for (int i = 0; i < Math.Min(5, pageUrls.Count); i++)
            {
                Console.WriteLine($"  {i + 1}. {pageUrls[i]}");
            }
            
            if (pageUrls.Count > 5)
            {
                Console.WriteLine($"  ... and {pageUrls.Count - 5} more");
            }
        }
        else
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Assert.That(result.SuccessCount, Is.GreaterThan(0), "Should find at least one page URL");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlChapters_FromManga_ShouldReturnChaptersList()
    {
        Console.WriteLine($"Test: CrawlChapters_FromManga_ShouldReturnChaptersList");
        Console.WriteLine($"Crawling chapters from: /truyen-tranh/one-piece");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        var result = await _crawler.CrawlChaptersAsync("/truyen-tranh/one-piece", context, maxChapters: 10);

        Console.WriteLine($"\nResult:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  Total Count: {result.TotalCount}");
        Console.WriteLine($"  Success Count: {result.SuccessCount}");

        if (result.IsSuccess && result.Data != null)
        {
            var chapters = result.Data.ToList();
            Console.WriteLine($"\nFound {chapters.Count} chapters:");
            
            foreach (var chapter in chapters.Take(5))
            {
                Console.WriteLine($"  - {chapter.Title} (Chapter #{chapter.ChapterNumber}, Index: {chapter.ChapterIndex})");
                Console.WriteLine($"    URL: {chapter.SourceUrl}");
            }
            
            if (chapters.Count > 5)
            {
                Console.WriteLine($"  ... and {chapters.Count - 5} more chapters");
            }
        }
        else
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Assert.That(result.Data, Is.Not.Null, "Data should not be null");
        Assert.That(result.SuccessCount, Is.GreaterThan(0), "Should find at least one chapter");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlNewChapters_ShouldOnlyReturnNewOnes()
    {
        Console.WriteLine($"Test: CrawlNewChapters_ShouldOnlyReturnNewOnes");
        Console.WriteLine($"Crawling new chapters from: /truyen-tranh/one-piece");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece"
        };

        // Simulate existing chapter IDs (giả sử đã có chapters 1165, 1164, 1163)
        var existingChapterIds = new[] { "2164491", "2164490", "2164489" }; // IDs giả định

        var result = await _crawler.CrawlNewChaptersAsync(
            "/truyen-tranh/one-piece",
            existingChapterIds,
            context);

        Console.WriteLine($"\nResult:");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  Total Count: {result.TotalCount}");
        Console.WriteLine($"  Success Count: {result.SuccessCount}");

        if (result.IsSuccess && result.Data != null)
        {
            var newChapters = result.Data.ToList();
            Console.WriteLine($"\nFound {newChapters.Count} new chapters:");
            
            foreach (var chapter in newChapters.Take(5))
            {
                Console.WriteLine($"  - {chapter.Title} (ID: {chapter.SourceChapterId})");
            }
        }
        else
        {
            Console.WriteLine($"  Error: {result.ErrorMessage}");
        }

        Assert.That(result.IsSuccess, Is.True, "Crawl should succeed");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task DownloadPage_ShouldDownloadImage()
    {
        Console.WriteLine($"Test: DownloadPage_ShouldDownloadImage");
        Console.WriteLine("Testing download single image");

        // First, get a page URL from a chapter
        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece/chapter-1165"
        };

        var pageUrlsResult = await _crawler.CrawlPageUrlsAsync("/truyen-tranh/one-piece/chapter-1165", context);
        
        if (!pageUrlsResult.IsSuccess || pageUrlsResult.Data == null || !pageUrlsResult.Data.Any())
        {
            Assert.Fail("Cannot get page URLs for testing");
            return;
        }

        var firstPageUrl = pageUrlsResult.Data.First();
        Console.WriteLine($"Downloading: {firstPageUrl}");

        var downloadResult = await _crawler.DownloadPageAsync(firstPageUrl, context);

        Console.WriteLine($"\nDownload Result:");
        Console.WriteLine($"  Success: {downloadResult.IsSuccess}");
        Console.WriteLine($"  URL: {downloadResult.Url}");

        if (downloadResult.IsSuccess && downloadResult.Data != null)
        {
            Console.WriteLine($"  Image Size: {downloadResult.Data.Length:N0} bytes ({downloadResult.Data.Length / 1024.0:F2} KB)");
        }
        else
        {
            Console.WriteLine($"  Error: {downloadResult.ErrorMessage}");
        }

        Assert.That(downloadResult.IsSuccess, Is.True, "Download should succeed");
        Assert.That(downloadResult.Data, Is.Not.Null, "Data should not be null");
        Assert.That(downloadResult.Data!.Length, Is.GreaterThan(0), "Image should have content");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task DownloadPages_MultipleImages_ShouldDownloadAll()
    {
        Console.WriteLine($"Test: DownloadPages_MultipleImages_ShouldDownloadAll");
        Console.WriteLine("Testing download multiple images");

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/truyen-tranh/one-piece/chapter-1165",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        // Get page URLs
        var pageUrlsResult = await _crawler.CrawlPageUrlsAsync("/truyen-tranh/one-piece/chapter-1165", context);
        
        if (!pageUrlsResult.IsSuccess || pageUrlsResult.Data == null || !pageUrlsResult.Data.Any())
        {
            Assert.Fail("Cannot get page URLs for testing");
            return;
        }

        // Download first 3 pages only (to avoid long test)
        var pageUrls = pageUrlsResult.Data.Take(3).ToList();
        Console.WriteLine($"Downloading {pageUrls.Count} pages...");

        var downloadResult = await _crawler.DownloadPagesAsync(pageUrls, context, maxPages: 3);

        Console.WriteLine($"\nDownload Result:");
        Console.WriteLine($"  Success: {downloadResult.IsSuccess}");
        Console.WriteLine($"  Total Count: {downloadResult.TotalCount}");
        Console.WriteLine($"  Success Count: {downloadResult.SuccessCount}");

        if (downloadResult.IsSuccess && downloadResult.Data != null)
        {
            var images = downloadResult.Data.ToList();
            Console.WriteLine($"\nDownloaded {images.Count} images:");
            
            for (int i = 0; i < images.Count; i++)
            {
                var size = images[i].Length;
                Console.WriteLine($"  {i + 1}. {size:N0} bytes ({size / 1024.0:F2} KB)");
            }
        }
        else
        {
            Console.WriteLine($"  Error: {downloadResult.ErrorMessage}");
        }

        Assert.That(downloadResult.IsSuccess, Is.True, "Download should succeed");
        Assert.That(downloadResult.Data, Is.Not.Null, "Data should not be null");
        Assert.That(downloadResult.SuccessCount, Is.GreaterThan(0), "Should download at least one image");
        Console.WriteLine("✓ Test passed\n");
    }

    [Test]
    public async Task CrawlChapter_MultipleChapters_ShouldWork()
    {
        Console.WriteLine($"Test: CrawlChapter_MultipleChapters_ShouldWork");
        
        var chapterUrls = new[]
        {
            "/truyen-tranh/one-piece/chapter-1165",
            "/truyen-tranh/one-piece/chapter-1164",
            "/truyen-tranh/one-piece/chapter-1163"
        };

        foreach (var chapterUrl in chapterUrls)
        {
            Console.WriteLine($"\nCrawling: {chapterUrl}");
            
            var context = new CrawlerContext
            {
                Source = _source,
                StartUrl = $"{_source.BaseUrl}{chapterUrl}"
            };

            var result = await _crawler.CrawlChapterAsync(chapterUrl, context);

            if (result.IsSuccess && result.Data != null)
            {
                var chapter = result.Data;
                Console.WriteLine($"  ✓ {chapter.Title}");
                Console.WriteLine($"    Pages: {chapter.PageUrls.Count}");
                Console.WriteLine($"    Chapter Index: {chapter.ChapterIndex}");
            }
            else
            {
                Console.WriteLine($"  ✗ Failed: {result.ErrorMessage}");
            }

            Assert.That(result.IsSuccess, Is.True, $"Should crawl {chapterUrl} successfully");
            
            // Delay để tránh rate limit
            await Task.Delay(2000);
        }

        Console.WriteLine("✓ Test passed\n");
    }
}

