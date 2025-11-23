using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infrastructure.Crawlers;
using SkyHighManga.Infrastructure.Services;

namespace SkyHighManga.UnitTest.Crawlers;

/// <summary>
/// Quick test để verify selector fix
/// </summary>
[TestFixture]
public class NettruyenCrawlerQuickTest
{
    [Test]
    public async Task TestSelector_WithRealHTML_ShouldFindItems()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Test: TestSelector_WithRealHTML_ShouldFindItems");
        Console.WriteLine();

        // Read HTML from file
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SkyHighManga.Application", "searchpage.html");
        if (!File.Exists(htmlPath))
        {
            // Try alternative path
            htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SkyHighManga.Application", "searchpage.html");
        }

        if (!File.Exists(htmlPath))
        {
            Assert.Fail($"Cannot find searchpage.html at: {htmlPath}");
            return;
        }

        Console.WriteLine($"Reading HTML from: {htmlPath}");
        var html = await File.ReadAllTextAsync(htmlPath);
        Console.WriteLine($"HTML length: {html.Length} chars");
        Console.WriteLine();

        var parser = new HtmlAgilityPackParser();
        var document = parser.Parse(html);

        // Test old selector (should fail)
        Console.WriteLine("Testing OLD selector: //div[@class='m-post']");
        var oldSelectorItems = document.QuerySelectorAll("//div[@class='m-post']").ToList();
        Console.WriteLine($"  Found: {oldSelectorItems.Count} items");
        Console.WriteLine();

        // Test new selector (should work)
        Console.WriteLine("Testing NEW selector: //div[contains(@class, 'm-post')]");
        var newSelectorItems = document.QuerySelectorAll("//div[contains(@class, 'm-post')]").ToList();
        Console.WriteLine($"  Found: {newSelectorItems.Count} items");
        Console.WriteLine();

        // Test with search-result wrapper
        Console.WriteLine("Testing with search-result wrapper:");
        var searchResultItems = document.QuerySelectorAll("//div[@class='search-result']//div[contains(@class, 'm-post')]").ToList();
        Console.WriteLine($"  Found: {searchResultItems.Count} items");
        Console.WriteLine();

        // Show first item details
        if (newSelectorItems.Count > 0)
        {
            Console.WriteLine("First item details:");
            var firstItem = newSelectorItems[0];
            
            var titleElement = firstItem.QuerySelector("//h3[@class='m-name']//a");
            var title = titleElement?.TextContent?.Trim();
            var titleUrl = titleElement?.GetAttribute("href");
            
            Console.WriteLine($"  Title: {title}");
            Console.WriteLine($"  URL: {titleUrl}");
            
            var imgElement = firstItem.QuerySelector("//img[@class='lzl']");
            var coverUrl = imgElement?.GetAttribute("data-src") 
                ?? imgElement?.GetAttribute("data-original")
                ?? imgElement?.GetAttribute("src");
            Console.WriteLine($"  Cover: {coverUrl}");
        }

        Console.WriteLine();
        Assert.That(newSelectorItems.Count, Is.GreaterThan(0), "Should find at least one manga item with new selector");
        Console.WriteLine("✓ Test passed");
        Console.WriteLine("=".PadRight(80, '='));
    }
}

