using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infrastructure.Crawlers;
using SkyHighManga.Infrastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// End-to-End tests cho NettruyenCrawler - test toàn bộ flow từ search đến crawl chapters
/// </summary>
[TestFixture]
public class NettruyenCrawlerE2ETests
{
    private NettruyenCrawler _crawler = null!;
    private Source _source = null!;
    private IHtmlParser _htmlParser = null!;

    [SetUp]
    public void Setup()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Setting up NettruyenCrawler End-to-End Tests");
        
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
    public async Task E2E_CompleteFlow_FromSearchToChapters()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("E2E Test: Complete Flow from Search to Chapters");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        // ==========================================
        // STEP 1: Search Manga
        // ==========================================
        Console.WriteLine("STEP 1: Searching for manga...");
        Console.WriteLine("-".PadRight(80, '-'));
        
        var searchKeyword = "One Piece";
        var searchResult = await _crawler.SearchMangaAsync(searchKeyword, context, maxResults: 5);

        Assert.That(searchResult.IsSuccess, Is.True, "Search should succeed");
        Assert.That(searchResult.Data, Is.Not.Null, "Search data should not be null");
        Assert.That(searchResult.SuccessCount, Is.GreaterThan(0), "Should find at least one manga");

        var searchMangas = searchResult.Data!.ToList();
        Console.WriteLine($"✓ Found {searchMangas.Count} manga(s)");
        
        foreach (var manga in searchMangas.Take(3))
        {
            Console.WriteLine($"  - {manga.Title} (URL: {manga.SourceUrl})");
        }
        Console.WriteLine();

        // Delay để tránh rate limit
        await Task.Delay(2000);

        // ==========================================
        // STEP 2: Select First Manga and Crawl Detail
        // ==========================================
        Console.WriteLine("STEP 2: Crawling manga detail...");
        Console.WriteLine("-".PadRight(80, '-'));

        var selectedManga = searchMangas.First();
        var mangaUrl = selectedManga.SourceUrl.Replace(_source.BaseUrl, "");
        
        Console.WriteLine($"Selected: {selectedManga.Title}");
        Console.WriteLine($"URL: {mangaUrl}");

        var mangaDetailResult = await _crawler.CrawlMangaAsync(mangaUrl, context);

        Assert.That(mangaDetailResult.IsSuccess, Is.True, "Manga detail crawl should succeed");
        Assert.That(mangaDetailResult.Data, Is.Not.Null, "Manga detail data should not be null");

        var mangaDetail = mangaDetailResult.Data!;
        Console.WriteLine($"✓ Manga Detail Crawled:");
        Console.WriteLine($"  Title: {mangaDetail.Title}");
        Console.WriteLine($"  Author: {mangaDetail.AuthorName}");
        Console.WriteLine($"  Rating: {mangaDetail.Rating}");
        Console.WriteLine($"  View Count: {mangaDetail.ViewCount:N0}");
        Console.WriteLine($"  Genres: {string.Join(", ", mangaDetail.Genres)}");
        Console.WriteLine($"  Description Length: {mangaDetail.Description?.Length ?? 0} chars");
        Console.WriteLine($"  Cover Image: {mangaDetail.CoverImageUrl}");
        Console.WriteLine($"  Chapters Found: {mangaDetail.Chapters.Count}");
        Console.WriteLine();

        // Delay để tránh rate limit
        await Task.Delay(2000);

        // ==========================================
        // STEP 3: Verify Chapters List
        // ==========================================
        Console.WriteLine("STEP 3: Verifying chapters list...");
        Console.WriteLine("-".PadRight(80, '-'));

        Assert.That(mangaDetail.Chapters.Count, Is.GreaterThan(0), "Should have at least one chapter");

        Console.WriteLine($"✓ Found {mangaDetail.Chapters.Count} chapters");
        Console.WriteLine("\nFirst 5 chapters:");
        foreach (var chapter in mangaDetail.Chapters.Take(5))
        {
            Console.WriteLine($"  - {chapter.Title} (Chapter #{chapter.ChapterNumber}, Index: {chapter.ChapterIndex})");
            Console.WriteLine($"    URL: {chapter.SourceUrl}");
        }
        Console.WriteLine();

        // ==========================================
        // STEP 4: Crawl Chapters List (Alternative Method)
        // ==========================================
        Console.WriteLine("STEP 4: Crawling chapters list using CrawlChaptersAsync...");
        Console.WriteLine("-".PadRight(80, '-'));

        var chaptersListResult = await _crawler.CrawlChaptersAsync(mangaUrl, context, maxChapters: 10);

        Assert.That(chaptersListResult.IsSuccess, Is.True, "Chapters list crawl should succeed");
        Assert.That(chaptersListResult.Data, Is.Not.Null, "Chapters list data should not be null");

        var chaptersList = chaptersListResult.Data!.ToList();
        Console.WriteLine($"✓ Crawled {chaptersList.Count} chapters using CrawlChaptersAsync");
        Console.WriteLine();

        // Delay để tránh rate limit
        await Task.Delay(2000);

        // ==========================================
        // STEP 5: Crawl Individual Chapters with Images
        // ==========================================
        Console.WriteLine("STEP 5: Crawling individual chapters with images...");
        Console.WriteLine("-".PadRight(80, '-'));

        // Chọn 2 chapters đầu tiên để test
        var chaptersToTest = mangaDetail.Chapters.Take(2).ToList();
        var allChapterData = new List<ChapterCrawlData>();

        for (int i = 0; i < chaptersToTest.Count; i++)
        {
            var chapter = chaptersToTest[i];
            var chapterUrl = chapter.SourceUrl.Replace(_source.BaseUrl, "");
            
            Console.WriteLine($"\n[{i + 1}/{chaptersToTest.Count}] Crawling: {chapter.Title}");
            Console.WriteLine($"  URL: {chapterUrl}");

            var chapterResult = await _crawler.CrawlChapterAsync(chapterUrl, context);

            Assert.That(chapterResult.IsSuccess, Is.True, $"Chapter {chapter.Title} crawl should succeed");
            Assert.That(chapterResult.Data, Is.Not.Null, $"Chapter {chapter.Title} data should not be null");

            var chapterData = chapterResult.Data!;
            allChapterData.Add(chapterData);

            Console.WriteLine($"  ✓ Chapter crawled:");
            Console.WriteLine($"    Title: {chapterData.Title}");
            Console.WriteLine($"    Chapter Number: {chapterData.ChapterNumber}");
            Console.WriteLine($"    Chapter Index: {chapterData.ChapterIndex}");
            Console.WriteLine($"    Source Chapter ID: {chapterData.SourceChapterId}");
            Console.WriteLine($"    Page URLs Count: {chapterData.PageUrls.Count}");

            if (chapterData.PageUrls.Count > 0)
            {
                Console.WriteLine($"    First Page URL: {chapterData.PageUrls.First()}");
                Console.WriteLine($"    Last Page URL: {chapterData.PageUrls.Last()}");
            }

            // Delay giữa các chapters
            if (i < chaptersToTest.Count - 1)
            {
                await Task.Delay(2000);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"✓ Successfully crawled {allChapterData.Count} chapters with images");
        Console.WriteLine();

        // ==========================================
        // STEP 6: Verify Page URLs
        // ==========================================
        Console.WriteLine("STEP 6: Verifying page URLs...");
        Console.WriteLine("-".PadRight(80, '-'));

        foreach (var chapterData in allChapterData)
        {
            Assert.That(chapterData.PageUrls.Count, Is.GreaterThan(0), 
                $"Chapter {chapterData.Title} should have at least one page URL");
            
            Console.WriteLine($"  ✓ {chapterData.Title}: {chapterData.PageUrls.Count} pages");
            
            // Verify URLs are valid
            foreach (var pageUrl in chapterData.PageUrls.Take(3))
            {
                Assert.That(pageUrl, Does.StartWith("http"), "Page URL should be absolute");
                Console.WriteLine($"    - {pageUrl}");
            }
        }
        Console.WriteLine();

        // ==========================================
        // STEP 7: Test Download Pages (Optional - chỉ download 1 page để test)
        // ==========================================
        Console.WriteLine("STEP 7: Testing page download (1 page only)...");
        Console.WriteLine("-".PadRight(80, '-'));

        if (allChapterData.Count > 0 && allChapterData[0].PageUrls.Count > 0)
        {
            var firstPageUrl = allChapterData[0].PageUrls.First();
            Console.WriteLine($"Downloading: {firstPageUrl}");

            var downloadResult = await _crawler.DownloadPageAsync(firstPageUrl, context);

            Assert.That(downloadResult.IsSuccess, Is.True, "Page download should succeed");
            Assert.That(downloadResult.Data, Is.Not.Null, "Downloaded data should not be null");
            Assert.That(downloadResult.Data!.Length, Is.GreaterThan(0), "Downloaded image should have content");

            Console.WriteLine($"  ✓ Downloaded: {downloadResult.Data.Length:N0} bytes ({downloadResult.Data.Length / 1024.0:F2} KB)");
        }
        else
        {
            Console.WriteLine("  ⚠ Skipped: No pages available to download");
        }
        Console.WriteLine();

        // ==========================================
        // SUMMARY
        // ==========================================
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("E2E Test Summary");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"✓ Step 1: Search - Found {searchMangas.Count} manga(s)");
        Console.WriteLine($"✓ Step 2: Manga Detail - Crawled '{mangaDetail.Title}'");
        Console.WriteLine($"✓ Step 3: Chapters List - Found {mangaDetail.Chapters.Count} chapters");
        Console.WriteLine($"✓ Step 4: Chapters List (Alt) - Crawled {chaptersList.Count} chapters");
        Console.WriteLine($"✓ Step 5: Individual Chapters - Crawled {allChapterData.Count} chapters with images");
        Console.WriteLine($"✓ Step 6: Page URLs - Verified {allChapterData.Sum(c => c.PageUrls.Count)} total pages");
        Console.WriteLine($"✓ Step 7: Page Download - Tested download functionality");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("✓ All steps completed successfully!");
        Console.WriteLine();
    }

    [Test]
    public async Task E2E_CompleteFlow_WithMultipleMangas()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("E2E Test: Complete Flow with Multiple Mangas");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem",
            OnLog = (msg, level) => Console.WriteLine($"  [{level}] {msg}"),
            OnProgress = (processed, total) => Console.WriteLine($"  Progress: {processed}/{total}")
        };

        // Search multiple keywords
        var keywords = new[] { "One Piece", "Naruto", "Dragon Ball" };
        var allMangaDetails = new List<MangaCrawlData>();

        foreach (var keyword in keywords)
        {
            Console.WriteLine($"\nSearching for: '{keyword}'");
            Console.WriteLine("-".PadRight(80, '-'));

            var searchResult = await _crawler.SearchMangaAsync(keyword, context, maxResults: 1);

            if (searchResult.IsSuccess && searchResult.Data != null && searchResult.Data.Any())
            {
                var manga = searchResult.Data.First();
                Console.WriteLine($"  Found: {manga.Title}");

                // Crawl manga detail
                var mangaUrl = manga.SourceUrl.Replace(_source.BaseUrl, "");
                var detailResult = await _crawler.CrawlMangaAsync(mangaUrl, context);

                if (detailResult.IsSuccess && detailResult.Data != null)
                {
                    var detail = detailResult.Data;
                    allMangaDetails.Add(detail);
                    Console.WriteLine($"  ✓ Detail: {detail.Title} - {detail.Chapters.Count} chapters");
                }

                await Task.Delay(2000);
            }
        }

        Console.WriteLine($"\n✓ Processed {allMangaDetails.Count} mangas");
        Console.WriteLine($"  Total chapters: {allMangaDetails.Sum(m => m.Chapters.Count)}");
        Console.WriteLine();

        Assert.That(allMangaDetails.Count, Is.GreaterThan(0), "Should process at least one manga");
    }

    [Test]
    public async Task E2E_VerifyDataConsistency()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("E2E Test: Verify Data Consistency");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        // Search
        var searchResult = await _crawler.SearchMangaAsync("One Piece", context, maxResults: 1);
        Assert.That(searchResult.IsSuccess, Is.True);

        var searchManga = searchResult.Data!.First();
        var mangaUrl = searchManga.SourceUrl.Replace(_source.BaseUrl, "");

        await Task.Delay(2000);

        // Crawl detail
        var detailResult = await _crawler.CrawlMangaAsync(mangaUrl, context);
        Assert.That(detailResult.IsSuccess, Is.True);

        var detailManga = detailResult.Data!;

        // Verify consistency
        Console.WriteLine("Verifying data consistency...");
        Console.WriteLine($"  Search Title: {searchManga.Title}");
        Console.WriteLine($"  Detail Title: {detailManga.Title}");
        Console.WriteLine($"  URLs Match: {searchManga.SourceUrl == detailManga.SourceUrl}");

        Assert.That(searchManga.Title, Is.EqualTo(detailManga.Title), "Titles should match");
        Assert.That(searchManga.SourceUrl, Is.EqualTo(detailManga.SourceUrl), "URLs should match");

        // Verify chapters from detail match chapters from CrawlChaptersAsync
        await Task.Delay(2000);
        var chaptersResult = await _crawler.CrawlChaptersAsync(mangaUrl, context, maxChapters: 5);
        Assert.That(chaptersResult.IsSuccess, Is.True);

        var chaptersList = chaptersResult.Data!.ToList();
        Console.WriteLine($"  Detail Chapters: {detailManga.Chapters.Count}");
        Console.WriteLine($"  Chapters List: {chaptersList.Count}");

        // Verify first few chapters match
        var detailChapters = detailManga.Chapters.Take(5).ToList();
        var listChapters = chaptersList.Take(5).ToList();

        for (int i = 0; i < Math.Min(detailChapters.Count, listChapters.Count); i++)
        {
            Console.WriteLine($"    Chapter {i + 1}:");
            Console.WriteLine($"      Detail: {detailChapters[i].Title} - {detailChapters[i].SourceUrl}");
            Console.WriteLine($"      List: {listChapters[i].Title} - {listChapters[i].SourceUrl}");
            
            Assert.That(detailChapters[i].Title, Is.EqualTo(listChapters[i].Title), 
                $"Chapter {i + 1} titles should match");
            Assert.That(detailChapters[i].SourceUrl, Is.EqualTo(listChapters[i].SourceUrl), 
                $"Chapter {i + 1} URLs should match");
        }

        Console.WriteLine("✓ Data consistency verified");
        Console.WriteLine();
    }

    [Test]
    public async Task E2E_PerformanceTest_MeasureCrawlTime()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("E2E Test: Performance Test - Measure Crawl Time");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var context = new CrawlerContext
        {
            Source = _source,
            StartUrl = $"{_source.BaseUrl}/tim-kiem"
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Search
        var searchStart = stopwatch.ElapsedMilliseconds;
        var searchResult = await _crawler.SearchMangaAsync("One Piece", context, maxResults: 1);
        var searchTime = stopwatch.ElapsedMilliseconds - searchStart;
        Console.WriteLine($"Step 1 - Search: {searchTime}ms");

        Assert.That(searchResult.IsSuccess, Is.True);
        var mangaUrl = searchResult.Data!.First().SourceUrl.Replace(_source.BaseUrl, "");

        await Task.Delay(2000);

        // Step 2: Manga Detail
        var detailStart = stopwatch.ElapsedMilliseconds;
        var detailResult = await _crawler.CrawlMangaAsync(mangaUrl, context);
        var detailTime = stopwatch.ElapsedMilliseconds - detailStart;
        Console.WriteLine($"Step 2 - Manga Detail: {detailTime}ms");

        Assert.That(detailResult.IsSuccess, Is.True);
        var mangaDetail = detailResult.Data!;

        await Task.Delay(2000);

        // Step 3: Crawl Chapter
        if (mangaDetail.Chapters.Count > 0)
        {
            var chapterUrl = mangaDetail.Chapters.First().SourceUrl.Replace(_source.BaseUrl, "");
            var chapterStart = stopwatch.ElapsedMilliseconds;
            var chapterResult = await _crawler.CrawlChapterAsync(chapterUrl, context);
            var chapterTime = stopwatch.ElapsedMilliseconds - chapterStart;
            Console.WriteLine($"Step 3 - Chapter: {chapterTime}ms");
            Console.WriteLine($"  Pages: {chapterResult.Data?.PageUrls.Count ?? 0}");
        }

        stopwatch.Stop();
        var totalTime = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"\nTotal Time: {totalTime}ms ({totalTime / 1000.0:F2}s)");
        Console.WriteLine();
    }
}

