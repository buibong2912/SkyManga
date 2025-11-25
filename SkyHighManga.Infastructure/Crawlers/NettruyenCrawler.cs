using SkyHighManga.Infastructure.Data;
using System.Collections.Concurrent;
using System.Threading;
using SkyHighManga.Application.Common;
using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Crawlers;

/// <summary>
/// Crawler cho Nettruyen (aquastarsleep.co.uk)
/// </summary>
public class NettruyenCrawler : BaseCrawler, IMangaCrawler, IChapterCrawler, IPageCrawler
{
    private readonly IHtmlParser _htmlParser;

    public NettruyenCrawler(
        IHtmlParser htmlParser,
        IRateLimiter? rateLimiter = null,
        Func<HttpClient>? httpClientFactory = null)
        : base(rateLimiter, httpClientFactory)
    {
        _htmlParser = htmlParser;
    }

    public override string Name => "NettruyenCrawler";

    public override SourceType SupportedSourceType => SourceType.Website;

    public override bool CanCrawl(Source source)
    {
        return base.CanCrawl(source) &&
               (source.BaseUrl.Contains("aquastarsleep.co.uk") ||
                source.BaseUrl.Contains("nettruyen"));
    }

    public async Task<CrawlerListResult<MangaCrawlData>> CrawlMangaListAsync(
        CrawlerContext context,
        int? maxItems = null)
    {
        try
        {
            Log(context, $"Bắt đầu crawl danh sách manga từ: {context.StartUrl}", Application.Common.Models.LogLevel.Info);

            var html = await DownloadHtmlAsync(context.StartUrl, context.CancellationToken);
            var document = _htmlParser.Parse(html);

            // Tìm tất cả các m-post items (class có thể là "m-post col-md-6" hoặc chỉ "m-post")
            var mangaItems = document.QuerySelectorAll("//div[contains(@class, 'm-post')]").ToList();

            var results = new List<MangaCrawlData>();
            var totalCount = mangaItems.Count;
            var max = maxItems ?? totalCount;

            Log(context, $"Tìm thấy {totalCount} manga items, sẽ crawl {max} items", Application.Common.Models.LogLevel.Info);

            for (int i = 0; i < Math.Min(max, totalCount); i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var item = mangaItems[i];
                var mangaData = ParseMangaItem(item, context.Source.BaseUrl);

                if (mangaData != null)
                {
                    results.Add(mangaData);
                    UpdateProgress(context, i + 1, max);
                }
            }

            Log(context, $"Hoàn thành crawl {results.Count} manga", Application.Common.Models.LogLevel.Info);

            return CrawlerListResult<MangaCrawlData>.Success(results, results.Count, context.StartUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl danh sách manga: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerListResult<MangaCrawlData>.Failure(
                $"Lỗi khi crawl danh sách manga: {ex.Message}",
                ex,
                context.StartUrl);
        }
    }

    public async Task<CrawlerResult<MangaCrawlData>> CrawlMangaAsync(
        string mangaUrl,
        CrawlerContext context)
    {
        try
        {
            Log(context, $"Bắt đầu crawl manga từ: {mangaUrl}", Application.Common.Models.LogLevel.Info);

            var fullUrl = BuildFullUrl(context.Source.BaseUrl, mangaUrl);
            var html = await DownloadHtmlAsync(fullUrl, context.CancellationToken);
            var document = _htmlParser.Parse(html);

            var mangaData = ParseMangaDetailPage(document, fullUrl);

            if (mangaData == null)
            {
                return CrawlerResult<MangaCrawlData>.Failure(
                    "Không thể parse thông tin manga",
                    null,
                    fullUrl);
            }

            Log(context, $"Hoàn thành crawl manga: {mangaData.Title}", Application.Common.Models.LogLevel.Info);

            return CrawlerResult<MangaCrawlData>.Success(mangaData, fullUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl manga: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerResult<MangaCrawlData>.Failure(
                $"Lỗi khi crawl manga: {ex.Message}",
                ex,
                mangaUrl);
        }
    }

    public async Task<CrawlerResult<MangaCrawlData>> CrawlMangaDetailsAsync(
        string mangaUrl,
        CrawlerContext context)
    {
        // Tương tự CrawlMangaAsync nhưng chỉ lấy thông tin chi tiết, không crawl chapters
        return await CrawlMangaAsync(mangaUrl, context);
    }

    public async Task<CrawlerListResult<MangaCrawlData>> SearchMangaAsync(
        string keyword,
        CrawlerContext context,
        int? maxResults = null,
        int? maxPages = null)
    {
        try
        {

            var baseSearchUrl = $"{context.Source.BaseUrl}/tim-kiem";
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                baseSearchUrl = $"{context.Source.BaseUrl}/tim-kiem?keyword={Uri.EscapeDataString(keyword)}";

            }
            Log(context, $"Tìm kiếm manga với keyword: {keyword}", Application.Common.Models.LogLevel.Info);
            Log(context, $"Base Search URL: {baseSearchUrl}", Application.Common.Models.LogLevel.Debug);

            var allResults = new ConcurrentBag<MangaCrawlData>();
            int totalPages = 1;
            int pagesToCrawl = 1; // Default, sẽ được cập nhật sau khi parse totalPages

            // Crawl trang đầu tiên để lấy thông tin pagination
            var firstPageUrl = baseSearchUrl;
            var html = await DownloadHtmlAsync(firstPageUrl, context.CancellationToken);
            var document = _htmlParser.Parse(html);

            // Parse tổng số trang từ pagination element
            var paginationElement = document.QuerySelector("//ul[@class='pagination']");
            if (paginationElement != null)
            {
                var countPageAttr = paginationElement.GetAttribute("data-count-page");
                if (!string.IsNullOrEmpty(countPageAttr) && int.TryParse(countPageAttr, out var countPage))
                {
                    totalPages = countPage;
                    Log(context, $"✅ Tìm thấy {totalPages} trang kết quả từ pagination", Application.Common.Models.LogLevel.Info);
                }
                else
                {
                    Log(context, $"⚠️ Không thể parse data-count-page từ pagination. Attribute value: '{countPageAttr}'", Application.Common.Models.LogLevel.Warning);
                }
            }
            else
            {
                Log(context, "⚠️ Không tìm thấy pagination element (ul[@class='pagination']). Có thể chỉ có 1 trang hoặc HTML structure khác.", Application.Common.Models.LogLevel.Warning);
            }
            
            // Nếu maxPages = null và totalPages = 1, có thể cần crawl thêm để kiểm tra
            // Nhưng trước tiên, log để debug
            if (maxPages == null && totalPages == 1)
            {
                Log(context, "⚠️ maxPages = null nhưng totalPages = 1. Có thể chỉ có 1 trang hoặc pagination chưa được parse đúng.", Application.Common.Models.LogLevel.Warning);
            }

            // Xác định số trang cần crawl
            if (maxPages == null)
            {
                // maxPages = null nghĩa là crawl TẤT CẢ các trang (tất cả pages của truyện)
                pagesToCrawl = totalPages;
                if (totalPages > 1)
                {
                    Log(context, $"✅ maxPages = null → sẽ crawl TẤT CẢ {totalPages} trang (tất cả pages của truyện)", Application.Common.Models.LogLevel.Info);
                }
                else
                {
                    Log(context, $"⚠️ maxPages = null nhưng chỉ có {totalPages} trang. Có thể pagination chưa được parse đúng hoặc thực sự chỉ có 1 trang.", Application.Common.Models.LogLevel.Warning);
                    Log(context, $"Sẽ crawl {pagesToCrawl} trang (có thể cần kiểm tra lại pagination parsing)", Application.Common.Models.LogLevel.Info);
                }
            }
            else if (maxPages <= 0)
            {
                // maxPages <= 0 nghĩa là crawl tất cả các trang (backward compatibility)
                pagesToCrawl = totalPages;
                Log(context, $"maxPages <= 0 → sẽ crawl TẤT CẢ {totalPages} trang", Application.Common.Models.LogLevel.Info);
            }
            else
            {
                // maxPages > 0 nghĩa là crawl tối đa maxPages trang
                pagesToCrawl = Math.Min(maxPages.Value, totalPages);
                Log(context, $"maxPages = {maxPages} → sẽ crawl tối đa {pagesToCrawl} trang (tổng {totalPages} trang)", Application.Common.Models.LogLevel.Info);
            }

            Log(context, $"🚀 Bắt đầu crawl {pagesToCrawl} trang song song...", Application.Common.Models.LogLevel.Info);

            var seenUrls = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            
            var estimatedTotalItems = pagesToCrawl * 10;
            var totalItemsTarget = maxResults ?? estimatedTotalItems;

            // Tối ưu: Crawl nhiều pages song song với multi-threading
            // Tăng concurrency lên cao hơn để crawl nhanh hơn (đặc biệt khi có nhiều pages)
            int maxConcurrency = pagesToCrawl > 100 ? 50 : (pagesToCrawl > 20 ? 20 : 10); // Tăng concurrency khi có nhiều pages
            Log(context, $"⚙️ Sử dụng concurrency: {maxConcurrency} pages đồng thời", Application.Common.Models.LogLevel.Info);
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var failedPages = new ConcurrentBag<int>();
            const int maxRetries = 3;
            var processedPages = 0;

            // Xử lý trang đầu tiên (đã có document)
            if (pagesToCrawl >= 1)
            {
                var firstPageResults = await CrawlPageAsync(1, baseSearchUrl, document, context, seenUrls, baseSearchUrl, 3);
                foreach (var mangaData in firstPageResults)
                {
                    if (maxResults.HasValue && allResults.Count >= maxResults.Value)
                        break;
                    allResults.Add(mangaData);
                }
                Interlocked.Increment(ref processedPages);
                UpdateProgress(context, allResults.Count, totalItemsTarget);
            }

            // Crawl các trang còn lại song song
            if (pagesToCrawl > 1)
            {
                Log(context, $"📥 Bắt đầu crawl {pagesToCrawl - 1} trang còn lại (từ trang 2 đến trang {pagesToCrawl})...", Application.Common.Models.LogLevel.Info);
                
                // Với số lượng pages lớn, sử dụng batch processing để tránh tạo quá nhiều tasks cùng lúc
                const int batchSize = 200; // Xử lý 200 pages mỗi batch
                var totalPagesToCrawl = pagesToCrawl - 1; // Trừ trang đầu tiên
                var totalBatches = (int)Math.Ceiling(totalPagesToCrawl / (double)batchSize);
                
                var lastProgressLog = DateTime.UtcNow;
                const int progressLogIntervalSeconds = 10; // Log progress mỗi 10 giây
                var totalProcessed = 0;
                
                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        break;

                    var startPage = 2 + (batchIndex * batchSize);
                    var endPage = Math.Min(startPage + batchSize - 1, pagesToCrawl);
                    var batchPages = endPage - startPage + 1;
                    
                    Log(context, $"📦 Batch {batchIndex + 1}/{totalBatches}: Crawl pages {startPage}-{endPage} ({batchPages} pages)...", 
                        Application.Common.Models.LogLevel.Info);
                    
                    var batchTasks = new List<Task>();
                    
                    for (int page = startPage; page <= endPage; page++)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            break;

                        // Kiểm tra nếu đã đủ maxResults
                        if (maxResults.HasValue && allResults.Count >= maxResults.Value)
                        {
                            Log(context, $"Đã đạt giới hạn {maxResults.Value} kết quả", Application.Common.Models.LogLevel.Info);
                            break;
                        }

                        var pageNum = page; // Capture để tránh closure issue
                        var task = CrawlPageInParallelAsync(
                            pageNum, 
                            baseSearchUrl, 
                            context, 
                            seenUrls, 
                            allResults,
                            semaphore,
                            failedPages,
                            maxRetries,
                            maxResults,
                            totalItemsTarget,
                            pagesToCrawl);
                        
                        batchTasks.Add(task);
                    }

                    // Đợi batch này hoàn thành trước khi chuyển sang batch tiếp theo
                    await Task.WhenAll(batchTasks);
                    
                    totalProcessed += batchTasks.Count;
                    var percent = (totalProcessed * 100.0) / totalPagesToCrawl;
                    
                    // Progress logging
                    if ((DateTime.UtcNow - lastProgressLog).TotalSeconds >= progressLogIntervalSeconds || batchIndex == totalBatches - 1)
                    {
                        Log(context, $"📊 Progress: Đã crawl {totalProcessed}/{totalPagesToCrawl} pages ({percent:F1}%), Đã tìm thấy: {allResults.Count} mangas", 
                            Application.Common.Models.LogLevel.Info);
                        lastProgressLog = DateTime.UtcNow;
                    }
                }

                Log(context, $"✅ Đã hoàn thành crawl {pagesToCrawl} trang. Tổng cộng {allResults.Count} mangas tìm được.", Application.Common.Models.LogLevel.Info);
            }
            else if (pagesToCrawl == 1)
            {
                Log(context, $"ℹ️ Chỉ crawl 1 trang (totalPages = {totalPages}). Tổng cộng {allResults.Count} mangas tìm được.", Application.Common.Models.LogLevel.Info);
                if (maxPages == null)
                {
                    Log(context, "⚠️ LƯU Ý: maxPages = null nhưng chỉ crawl được 1 trang. Có thể:", Application.Common.Models.LogLevel.Warning);
                    Log(context, "   1. Pagination element không được tìm thấy hoặc parse không đúng", Application.Common.Models.LogLevel.Warning);
                    Log(context, "   2. Thực sự chỉ có 1 trang kết quả", Application.Common.Models.LogLevel.Warning);
                    Log(context, "   → Kiểm tra logs phía trên để xem pagination có được parse đúng không", Application.Common.Models.LogLevel.Warning);
                }
            }
            else if (pagesToCrawl == 1)
            {
                Log(context, $"ℹ️ Chỉ crawl 1 trang (totalPages = {totalPages}). Tổng cộng {allResults.Count} mangas tìm được.", Application.Common.Models.LogLevel.Info);
            }

            if (failedPages.Count > 0)
            {
                Log(context, $"Cảnh báo: {failedPages.Count} trang bị lỗi trong quá trình crawl", Application.Common.Models.LogLevel.Warning);
            }
          

            // Giới hạn kết quả theo maxResults nếu có
            var finalResults = allResults.ToList();
            if (maxResults.HasValue && finalResults.Count > maxResults.Value)
            {
                finalResults = finalResults.Take(maxResults.Value).ToList();
            }

            Log(context, $"Hoàn thành tìm kiếm: {finalResults.Count} kết quả từ {pagesToCrawl} trang", Application.Common.Models.LogLevel.Info);

            return CrawlerListResult<MangaCrawlData>.Success(finalResults, finalResults.Count, baseSearchUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi tìm kiếm manga: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerListResult<MangaCrawlData>.Failure(
                $"Lỗi khi tìm kiếm manga: {ex.Message}",
                ex,
                context.StartUrl);
        }
    }


    /// <summary>
    /// Crawl một page và trả về danh sách manga items (cho trang đầu tiên đã có document)
    /// </summary>
    private async Task<List<MangaCrawlData>> CrawlPageAsync(
        int page,
        string baseSearchUrl,
        IHtmlDocument document,
        CrawlerContext context,
        ConcurrentDictionary<string, bool> seenUrls,
        string baseUrl, int maxRetries)
    {
        var results = new List<MangaCrawlData>();
        
        try
        {
            // Parse manga items từ trang này
            var mangaItems = document.QuerySelectorAll("//div[@class='search-result']//div[contains(@class, 'm-post')]").ToList();
            if (mangaItems.Count == 0)
            {
                mangaItems = document.QuerySelectorAll("//div[contains(@class, 'm-post')]").ToList();
            }

            foreach (var item in mangaItems)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var mangaData = ParseMangaItem(item, baseUrl);
                    if (mangaData != null && !string.IsNullOrEmpty(mangaData.SourceUrl))
                    {
                        if (seenUrls.TryAdd(mangaData.SourceUrl, true))
                        {
                            results.Add(mangaData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(context, $"Lỗi khi parse manga item ở trang {page}: {ex.Message}", Application.Common.Models.LogLevel.Warning);
                }
            }

            Log(context, $"Trang {page}: Tìm thấy {results.Count} manga items mới", Application.Common.Models.LogLevel.Info);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl trang {page}: {ex.Message}", Application.Common.Models.LogLevel.Warning);
        }

        return results;
    }

    /// <summary>
    /// Crawl một page song song với các pages khác (cho các trang từ 2 trở đi)
    /// </summary>
    private async Task CrawlPageInParallelAsync(
        int page,
        string baseSearchUrl,
        CrawlerContext context,
        ConcurrentDictionary<string, bool> seenUrls,
        ConcurrentBag<MangaCrawlData> allResults,
        SemaphoreSlim semaphore,
        ConcurrentBag<int> failedPages,
        int maxRetries,
        int? maxResults,
        int totalItemsTarget,
        int pagesToCrawl)
    {
        await semaphore.WaitAsync(context.CancellationToken);
        
        try
        {
            // Kiểm tra nếu đã đủ maxResults
            if (maxResults.HasValue && allResults.Count >= maxResults.Value)
            {
                return;
            }

            // Build URL cho trang này
            var separator = baseSearchUrl.Contains('?') ? "&" : "?";
            var pageUrl = $"{baseSearchUrl}{separator}page={page}";

            // Retry logic
            bool pageSuccess = false;
            int retryCount = 0;
            List<IHtmlElement>? mangaItems = null;

            while (!pageSuccess && retryCount < 3)
            {
                try
                {
                    var html = await DownloadHtmlAsync(pageUrl, context.CancellationToken);
                    var document = _htmlParser.Parse(html);

                    // Parse manga items
                    mangaItems = document.QuerySelectorAll("//div[@class='search-result']//div[contains(@class, 'm-post')]").ToList();
                    if (mangaItems.Count == 0)
                    {
                        mangaItems = document.QuerySelectorAll("//div[contains(@class, 'm-post')]").ToList();
                    }
                    pageSuccess = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount < 3)
                    {
                        Log(context, $"Lỗi khi crawl trang {page}, retry {retryCount}/{3}: {ex.Message}", Application.Common.Models.LogLevel.Warning);
                        await Task.Delay(2000 * retryCount, context.CancellationToken);
                    }
                    else
                    {
                        Log(context, $"Lỗi khi crawl trang {page} sau {3} lần thử: {ex.Message}", Application.Common.Models.LogLevel.Error);
                        failedPages.Add(page);
                    }
                }
            }

            if (!pageSuccess || mangaItems == null)
            {
                Log(context, $"Trang {page}: Không thể crawl hoặc không tìm thấy items", Application.Common.Models.LogLevel.Warning);
                return;
            }

            // Parse từng manga item
            int itemsAddedThisPage = 0;
            foreach (var item in mangaItems)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                if (maxResults.HasValue && allResults.Count >= maxResults.Value)
                    break;

                try
                {
                    var mangaData = ParseMangaItem(item, context.Source.BaseUrl);
                    if (mangaData != null && !string.IsNullOrEmpty(mangaData.SourceUrl))
                    {
                        if (seenUrls.TryAdd(mangaData.SourceUrl, true))
                        {
                            allResults.Add(mangaData);
                            itemsAddedThisPage++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(context, $"Lỗi khi parse manga item ở trang {page}: {ex.Message}", Application.Common.Models.LogLevel.Warning);
                }
            }

            // Log progress
            if (page % 10 == 0)
            {
                var progressPercent = pagesToCrawl > 0 ? (allResults.Count * 100.0 / (pagesToCrawl * 10)) : 0;
                Log(context, $"Progress: {allResults.Count}/{pagesToCrawl} trang ({progressPercent:F1}%) - {allResults.Count} items đã crawl", Application.Common.Models.LogLevel.Info);
            }
            else
            {
                Log(context, $"Trang {page}: Tìm thấy {itemsAddedThisPage} manga items mới (tổng: {allResults.Count})", Application.Common.Models.LogLevel.Debug);
            }

            // Update progress
            if (itemsAddedThisPage > 0)
            {
                UpdateProgress(context, allResults.Count, totalItemsTarget);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Parse một manga item từ search result hoặc list page
    /// </summary>
    private MangaCrawlData? ParseMangaItem(IHtmlElement item, string baseUrl)
    {
        try
        {
            var mangaData = new MangaCrawlData();

            // Lấy title và URL - sử dụng relative path (bắt đầu với .//) để tìm trong item hiện tại
            var titleElement = item.QuerySelector(".//h3[contains(@class, 'm-name')]//a");
            if (titleElement != null)
            {
                mangaData.Title = titleElement.TextContent?.Trim() ?? "";
                var href = titleElement.GetAttribute("href") ?? "";
                mangaData.SourceUrl = BuildFullUrl(baseUrl, href);
                
                // Extract SourceMangaId từ URL
                mangaData.SourceMangaId = ExtractMangaIdFromUrl(mangaData.SourceUrl);
            }

            // Lấy cover image - sử dụng relative path
            var imgElement = item.QuerySelector(".//img[contains(@class, 'lzl')]");
            if (imgElement != null)
            {
                // Ưu tiên data-src, sau đó data-original, cuối cùng là src
                mangaData.CoverImageUrl = imgElement.GetAttribute("data-src") 
                    ?? imgElement.GetAttribute("data-original")
                    ?? imgElement.GetAttribute("src");
                
                if (!string.IsNullOrEmpty(mangaData.CoverImageUrl) && !mangaData.CoverImageUrl.StartsWith("http"))
                {
                    mangaData.CoverImageUrl = BuildFullUrl(baseUrl, mangaData.CoverImageUrl);
                }
            }

            // Lấy rating - rating nằm trong span con trực tiếp của div.m-star (không có class)
            // Cấu trúc: <div class="m-star"><span class="star-rating">...</span><span>4.2</span></div>
            // Sử dụng relative path
            var mStarDiv = item.QuerySelector(".//div[contains(@class, 'm-star')]");
            if (mStarDiv != null)
            {
                var allSpans = mStarDiv.QuerySelectorAll("./span").ToList();
                // Tìm span không có class attribute (rating span)
                var ratingSpan = allSpans.FirstOrDefault(s => string.IsNullOrEmpty(s.GetAttribute("class")));
                
                if (ratingSpan != null)
                {
                    var ratingText = ratingSpan.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(ratingText) && double.TryParse(ratingText, out var rating))
                    {
                        mangaData.Rating = rating;
                    }
                }
            }

            // Lấy view count - sử dụng relative path
            var viewElement = item.QuerySelector(".//span[contains(@class, 'num-view')]");
            if (viewElement != null)
            {
                var viewText = viewElement.TextContent?.Trim() ?? "";
                mangaData.ViewCount = ParseViewCount(viewText);
            }

            // Lấy chapters - sử dụng relative path
            var chapterElements = item.QuerySelectorAll(".//ul[contains(@class, 'list-chaps')]//li[contains(@class, 'chapter')]//a").ToList();
            foreach (var chapterElement in chapterElements)
            {
                var chapterUrl = chapterElement.GetAttribute("href");
                if (!string.IsNullOrEmpty(chapterUrl))
                {
                    var chapterData = new ChapterCrawlData
                    {
                        SourceUrl = BuildFullUrl(baseUrl, chapterUrl),
                        Title = chapterElement.TextContent?.Trim() ?? ""
                    };

                    // Extract chapter number từ title
                    var chapterMatch = System.Text.RegularExpressions.Regex.Match(
                        chapterData.Title, 
                        @"Chapter\s*#?(\d+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (chapterMatch.Success && int.TryParse(chapterMatch.Groups[1].Value, out var chapterNum))
                    {
                        chapterData.ChapterNumber = chapterMatch.Groups[1].Value;
                        chapterData.ChapterIndex = chapterNum;
                        // Set SourceChapterId từ chapter number
                        if (string.IsNullOrEmpty(chapterData.SourceChapterId))
                        {
                            chapterData.SourceChapterId = chapterMatch.Groups[1].Value;
                        }
                    }
                    else
                    {
                        // Thử extract từ URL
                        var urlMatch = System.Text.RegularExpressions.Regex.Match(
                            chapterUrl,
                            @"chapter[_-]?(\d+)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (urlMatch.Success)
                        {
                            chapterData.SourceChapterId = urlMatch.Groups[1].Value;
                            chapterData.ChapterNumber = urlMatch.Groups[1].Value;
                            if (int.TryParse(urlMatch.Groups[1].Value, out var idx))
                            {
                                chapterData.ChapterIndex = idx;
                            }
                        }
                        else
                        {
                            // Fallback: sử dụng URL làm SourceChapterId
                            chapterData.SourceChapterId = chapterUrl.Trim('/').Split('/').LastOrDefault() ?? chapterUrl;
                        }
                    }

                    mangaData.Chapters.Add(chapterData);
                }
            }

            return mangaData;
        }
        catch
        {
            // Log error nhưng không throw, chỉ return null
            return null;
        }
    }

    /// <summary>
    /// Parse manga detail page
    /// </summary>
    private MangaCrawlData? ParseMangaDetailPage(IHtmlDocument document, string url)
    {
        try
        {
            var mangaData = new MangaCrawlData
            {
                SourceUrl = url
            };

            // Extract SourceMangaId từ URL (ví dụ: /truyen-tranh/one-piece -> "one-piece")
            mangaData.SourceMangaId = ExtractMangaIdFromUrl(url);

            // Thử lấy từ script tag nếu có (mangaDetail.id) - ưu tiên hơn URL
            var mangaDetailScript = document.QuerySelector("//script[contains(text(), 'mangaDetail')]");
            if (mangaDetailScript != null)
            {
                var scriptText = mangaDetailScript.TextContent ?? "";
                var idMatch = System.Text.RegularExpressions.Regex.Match(
                    scriptText,
                    @"""id""\s*:\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (idMatch.Success)
                {
                    mangaData.SourceMangaId = idMatch.Groups[1].Value;
                }
            }

            // Lấy title - <h1 class="title title-detail"><a>One Piece</a></h1>
            var titleElement = document.QuerySelector("//h1[contains(@class, 'title-detail')]//a")
                ?? document.QuerySelector("//h1[contains(@class, 'title-detail')]")
                ?? document.QuerySelector("//h1");
            if (titleElement != null)
            {
                mangaData.Title = titleElement.TextContent?.Trim() ?? "";
            }

            // Lấy rating - <div class="m-star"><span class="star-rating">...</span><span>5</span></div>
            var mStarDiv = document.QuerySelector("//div[contains(@class, 'm-star')]");
            if (mStarDiv != null)
            {
                var allSpans = mStarDiv.QuerySelectorAll("./span").ToList();
                var ratingSpan = allSpans.FirstOrDefault(s => string.IsNullOrEmpty(s.GetAttribute("class")));
                if (ratingSpan != null)
                {
                    var ratingText = ratingSpan.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(ratingText) && double.TryParse(ratingText, out var rating))
                    {
                        mangaData.Rating = rating;
                    }
                }
            }

            // Lấy thể loại - <div class="kind"><span class="label">Thể loại: </span><a href="/the-loai/manga">Manga</a></div>
            var kindElement = document.QuerySelector("//div[contains(@class, 'kind')]//a");
            if (kindElement != null)
            {
                var kindText = kindElement.TextContent?.Trim();
                if (!string.IsNullOrEmpty(kindText))
                {
                    mangaData.Genres.Add(kindText);
                }
            }

            // Lấy tags/genres - <div class="m-tags"><span class="label">Tags:</span><a href="/genre/shounen">Shounen</a>...</div>
            var tagElements = document.QuerySelectorAll("//div[contains(@class, 'm-tags')]//a[contains(@href, '/genre/')]").ToList();
            foreach (var tagElement in tagElements)
            {
                var tagName = tagElement.TextContent?.Trim();
                if (!string.IsNullOrEmpty(tagName))
                {
                    mangaData.Genres.Add(tagName);
                }
            }

            // Lấy tình trạng - <div class="status"><span>Tình trạng: </span><p>Đang tiến hành</p></div>
            var statusElement = document.QuerySelector("//div[contains(@class, 'status')]//p");
            if (statusElement != null)
            {
                var statusText = statusElement.TextContent?.Trim();
            }

            // Lấy tác giả - <div class="author"><span>Tác giả: </span><p>Eiichiro Oda</p></div>
            var authorElement = document.QuerySelector("//div[contains(@class, 'author')]//p");
            if (authorElement != null)
            {
                var authorName = authorElement.TextContent?.Trim();
                if (!string.IsNullOrEmpty(authorName))
                {
                    mangaData.AuthorName = authorName;
                }
            }

            // Lấy lượt xem - <div class="view"><span>Lượt xem: </span><p>80.172</p></div>
            var viewElement = document.QuerySelector("//div[contains(@class, 'view')]//p");
            if (viewElement != null)
            {
                var viewText = viewElement.TextContent?.Trim();
                if (!string.IsNullOrEmpty(viewText))
                {
                    // Parse số lượt xem (có thể có dấu chấm như "80.172")
                    viewText = viewText.Replace(".", "");
                    if (int.TryParse(viewText, out var viewCount))
                    {
                        mangaData.ViewCount = viewCount;
                    }
                }
            }

            // Lấy description - <div class="sort-des"><h2>...</h2><div class="line-clamp html-content">...</div></div>
            var descElement = document.QuerySelector("//div[contains(@class, 'sort-des')]//div[contains(@class, 'html-content')]")
                ?? document.QuerySelector("//div[contains(@class, 'sort-des')]//div[contains(@class, 'line-clamp')]");
            if (descElement != null)
            {
                mangaData.Description = descElement.TextContent?.Trim();
            }

            // Lấy cover image - có thể từ posterPath trong script tag hoặc từ img element
            // Thử lấy từ script tag trước (mangaDetail.posterPath)
            var scriptElement = document.QuerySelector("//script[contains(text(), 'mangaDetail')]");
            if (scriptElement != null)
            {
                var scriptText = scriptElement.TextContent ?? "";
                var posterMatch = System.Text.RegularExpressions.Regex.Match(
                    scriptText,
                    @"posterPath[""']?\s*:\s*[""']([^""']+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (posterMatch.Success)
                {
                    mangaData.CoverImageUrl = posterMatch.Groups[1].Value;
                }
            }

            // Nếu không có từ script, thử lấy từ img element
            if (string.IsNullOrEmpty(mangaData.CoverImageUrl))
            {
                var coverImg = document.QuerySelector("//div[contains(@class, 'col-thumb')]//img")
                    ?? document.QuerySelector("//img[contains(@class, 'poster')]");
                if (coverImg != null)
                {
                    mangaData.CoverImageUrl = coverImg.GetAttribute("src")
                        ?? coverImg.GetAttribute("data-src")
                        ?? coverImg.GetAttribute("data-original");
                }
            }

            // Lấy danh sách chapters - thử nhiều selector khác nhau
            var chapterElements = document.QuerySelectorAll("//div[contains(@class, 'list-chapters')]//div[contains(@class, 'l-chapter')]//a[contains(@class, 'll-chap')]").ToList();
            
            // Fallback: thử selector đơn giản hơn
            if (chapterElements.Count == 0)
            {
                chapterElements = document.QuerySelectorAll("//div[contains(@class, 'list-chapters')]//a[contains(@href, 'chapter')]").ToList();
            }
            
            // Fallback: thử selector khác
            if (chapterElements.Count == 0)
            {
                chapterElements = document.QuerySelectorAll("//div[contains(@class, 'list-chapters')]//a").ToList();
            }
            
            // Log để debug
            if (chapterElements.Count == 0)
            {
                // Thử tìm bất kỳ link nào có chứa "chapter" trong href
                chapterElements = document.QuerySelectorAll("//a[contains(@href, 'chapter')]").ToList();
            }
            
            foreach (var chapterElement in chapterElements)
            {
                var chapterUrl = chapterElement.GetAttribute("href");
                if (!string.IsNullOrEmpty(chapterUrl))
                {
                    var chapterData = new ChapterCrawlData
                    {
                        SourceUrl = BuildFullUrl(url, chapterUrl),
                        Title = chapterElement.TextContent?.Trim() ?? ""
                    };

                    // Extract chapter number từ title hoặc URL
                    var chapterMatch = System.Text.RegularExpressions.Regex.Match(
                        chapterData.Title,
                        @"Chapter\s*#?(\d+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (!chapterMatch.Success)
                    {
                        // Thử extract từ URL
                        chapterMatch = System.Text.RegularExpressions.Regex.Match(
                            chapterUrl,
                            @"chapter-(\d+)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    }

                    if (chapterMatch.Success && int.TryParse(chapterMatch.Groups[1].Value, out var chapterNum))
                    {
                        chapterData.ChapterNumber = chapterMatch.Groups[1].Value;
                        chapterData.ChapterIndex = chapterNum;
                        // Set SourceChapterId từ chapter number
                        if (string.IsNullOrEmpty(chapterData.SourceChapterId))
                        {
                            chapterData.SourceChapterId = chapterMatch.Groups[1].Value;
                        }
                    }
                    else
                    {
                        // Thử extract từ URL với pattern khác
                        var urlMatch = System.Text.RegularExpressions.Regex.Match(
                            chapterUrl,
                            @"chapter[_-]?(\d+)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (urlMatch.Success)
                        {
                            chapterData.SourceChapterId = urlMatch.Groups[1].Value;
                            chapterData.ChapterNumber = urlMatch.Groups[1].Value;
                            if (int.TryParse(urlMatch.Groups[1].Value, out var idx))
                            {
                                chapterData.ChapterIndex = idx;
                            }
                        }
                        else
                        {
                            // Fallback: sử dụng URL làm SourceChapterId
                            chapterData.SourceChapterId = chapterUrl.Trim('/').Split('/').LastOrDefault() ?? chapterUrl;
                        }
                    }

                    mangaData.Chapters.Add(chapterData);
                }
            }

            return mangaData;
        }
        catch
        {
            // Log error nếu có context
            return null;
        }
    }

    /// <summary>
    /// Extract manga ID từ URL (ví dụ: /truyen-tranh/one-piece -> "one-piece")
    /// </summary>
    private string? ExtractMangaIdFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            // Pattern: /truyen-tranh/{manga-id} hoặc /truyen-tranh/{manga-id}/...
            var match = System.Text.RegularExpressions.Regex.Match(
                url,
                @"/truyen-tranh/([^/?#]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Fallback: lấy phần cuối của URL path
            var uri = new Uri(url);
            var segments = uri.Segments;
            for (int i = segments.Length - 1; i >= 0; i--)
            {
                var segment = segments[i].Trim('/');
                if (!string.IsNullOrEmpty(segment) && 
                    !segment.Equals("truyen-tranh", StringComparison.OrdinalIgnoreCase) &&
                    !segment.StartsWith("chapter", StringComparison.OrdinalIgnoreCase))
                {
                    return segment;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    /// <summary>
    /// Parse view count từ text (ví dụ: "10.24M lượt xem" -> 10240000)
    /// </summary>
    private int ParseViewCount(string viewText)
    {
        if (string.IsNullOrWhiteSpace(viewText))
            return 0;

        try
        {
            // Remove "lượt xem" và trim
            viewText = viewText.Replace("lượt xem", "").Trim();

            // Parse số với K, M suffix
            var match = System.Text.RegularExpressions.Regex.Match(
                viewText,
                @"([\d.]+)\s*([KMkm]?)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && double.TryParse(match.Groups[1].Value, out var number))
            {
                var suffix = match.Groups[2].Value.ToUpper();
                return suffix switch
                {
                    "K" => (int)(number * 1000),
                    "M" => (int)(number * 1000000),
                    _ => (int)number
                };
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return 0;
    }

    #region IChapterCrawler Implementation

    public async Task<CrawlerListResult<ChapterCrawlData>> CrawlChaptersAsync(
        string mangaUrl,
        CrawlerContext context,
        int? maxChapters = null)
    {
        try
        {
            Log(context, $"Bắt đầu crawl chapters từ: {mangaUrl}", Application.Common.Models.LogLevel.Info);

            // Crawl manga detail để lấy danh sách chapters
            var mangaResult = await CrawlMangaAsync(mangaUrl, context);
            if (!mangaResult.IsSuccess || mangaResult.Data == null)
            {
                return CrawlerListResult<ChapterCrawlData>.Failure(
                    "Không thể crawl manga để lấy chapters",
                    mangaResult.Exception,
                    mangaUrl);
            }

            var chapters = mangaResult.Data.Chapters.ToList();
            var max = maxChapters ?? chapters.Count;

            Log(context, $"Tìm thấy {chapters.Count} chapters, sẽ crawl {max} chapters", Application.Common.Models.LogLevel.Info);

            var results = chapters.Take(max).ToList();

            return CrawlerListResult<ChapterCrawlData>.Success(results, results.Count, mangaUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl chapters: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerListResult<ChapterCrawlData>.Failure(
                $"Lỗi khi crawl chapters: {ex.Message}",
                ex,
                mangaUrl);
        }
    }

    public async Task<CrawlerResult<ChapterCrawlData>> CrawlChapterAsync(
        string chapterUrl,
        CrawlerContext context)
    {
        try
        {
            Log(context, $"Bắt đầu crawl chapter từ: {chapterUrl}", Application.Common.Models.LogLevel.Info);

            var fullUrl = BuildFullUrl(context.Source.BaseUrl, chapterUrl);
            var html = await DownloadHtmlAsync(fullUrl, context.CancellationToken);
            var document = _htmlParser.Parse(html);

            var chapterData = ParseChapterPage(document, fullUrl);

            if (chapterData == null)
            {
                return CrawlerResult<ChapterCrawlData>.Failure(
                    "Không thể parse thông tin chapter",
                    null,
                    fullUrl);
            }

            Log(context, $"Hoàn thành crawl chapter: {chapterData.Title} ({chapterData.PageUrls.Count} pages)", Application.Common.Models.LogLevel.Info);

            return CrawlerResult<ChapterCrawlData>.Success(chapterData, fullUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl chapter: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerResult<ChapterCrawlData>.Failure(
                $"Lỗi khi crawl chapter: {ex.Message}",
                ex,
                chapterUrl);
        }
    }

    public async Task<CrawlerListResult<ChapterCrawlData>> CrawlNewChaptersAsync(
        string mangaUrl,
        IEnumerable<string> existingChapterIds,
        CrawlerContext context)
    {
        try
        {
            var existingIds = existingChapterIds.ToHashSet();
            var allChaptersResult = await CrawlChaptersAsync(mangaUrl, context);

            if (!allChaptersResult.IsSuccess || allChaptersResult.Data == null)
            {
                return allChaptersResult;
            }

            var newChapters = allChaptersResult.Data
                .Where(c => !existingIds.Contains(c.SourceChapterId ?? ""))
                .ToList();

            Log(context, $"Tìm thấy {newChapters.Count} chapters mới", Application.Common.Models.LogLevel.Info);

            return CrawlerListResult<ChapterCrawlData>.Success(newChapters, newChapters.Count, mangaUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl chapters mới: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerListResult<ChapterCrawlData>.Failure(
                $"Lỗi khi crawl chapters mới: {ex.Message}",
                ex,
                mangaUrl);
        }
    }

    /// <summary>
    /// Parse chapter page để lấy thông tin chapter và danh sách image URLs
    /// </summary>
    private ChapterCrawlData? ParseChapterPage(IHtmlDocument document, string url)
    {
        try
        {
            var chapterData = new ChapterCrawlData
            {
                SourceUrl = url
            };

            // Lấy title từ script tag hoặc HTML
            var scriptElement = document.QuerySelector("//script[contains(text(), 'chapterDetail')]");
            if (scriptElement != null)
            {
                var scriptText = scriptElement.TextContent ?? "";
                
                // Parse chapterDetail JSON
                var nameMatch = System.Text.RegularExpressions.Regex.Match(
                    scriptText,
                    @"""name""\s*:\s*""([^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    chapterData.Title = nameMatch.Groups[1].Value;
                }

                var indexMatch = System.Text.RegularExpressions.Regex.Match(
                    scriptText,
                    @"""index""\s*:\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (indexMatch.Success && int.TryParse(indexMatch.Groups[1].Value, out var index))
                {
                    chapterData.ChapterIndex = index;
                    chapterData.ChapterNumber = indexMatch.Groups[1].Value;
                }

                var idMatch = System.Text.RegularExpressions.Regex.Match(
                    scriptText,
                    @"""id""\s*:\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (idMatch.Success)
                {
                    chapterData.SourceChapterId = idMatch.Groups[1].Value;
                }

                // Parse images array từ script tag (ưu tiên)
                var imagesMatch = System.Text.RegularExpressions.Regex.Match(
                    scriptText,
                    @"""images""\s*:\s*\[(.*?)\]",
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (imagesMatch.Success)
                {
                    var imagesJson = imagesMatch.Groups[1].Value;
                    
                    // Parse cả index và path để sort đúng thứ tự
                    var fullImageMatches = System.Text.RegularExpressions.Regex.Matches(
                        imagesJson,
                        @"""index""\s*:\s*(\d+).*?""path""\s*:\s*""([^""]+)""",
                        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    var imageList = new List<(int index, string path)>();

                    foreach (System.Text.RegularExpressions.Match match in fullImageMatches)
                    {
                        if (int.TryParse(match.Groups[1].Value, out var imgIndex))
                        {
                            imageList.Add((imgIndex, match.Groups[2].Value));
                        }
                    }

                    // Sort theo index và lấy paths
                    chapterData.PageUrls = imageList
                        .OrderBy(x => x.index)
                        .Select(x => x.path)
                        .ToList();
                }
            }

            // Nếu không parse được từ script, thử parse từ HTML img elements
            if (chapterData.PageUrls.Count == 0)
            {
                var imgElements = document.QuerySelectorAll("//div[@id='read-chaps']//img[contains(@class, 'reading-img')]").ToList();
                
                var imageList = new List<(int index, string url)>();
                
                foreach (var imgElement in imgElements)
                {
                    // Lấy data-indexr để sort
                    var indexAttr = imgElement.GetAttribute("data-indexr");
                    var imageUrl = imgElement.GetAttribute("src")
                        ?? imgElement.GetAttribute("data-src")
                        ?? imgElement.GetAttribute("data-original");

                    if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.Contains("/images/pre-load"))
                    {
                        if (int.TryParse(indexAttr, out var pageIndex))
                        {
                            imageList.Add((pageIndex, imageUrl));
                        }
                        else
                        {
                            imageList.Add((int.MaxValue, imageUrl)); // Add to end if no index
                        }
                    }
                }

                // Sort và extract URLs
                chapterData.PageUrls = imageList
                    .OrderBy(x => x.index)
                    .Select(x => x.url)
                    .ToList();
            }

            // Nếu vẫn không có title, thử lấy từ HTML
            if (string.IsNullOrEmpty(chapterData.Title))
            {
                var titleElement = document.QuerySelector("//h1[contains(@class, 'manga-name')]")
                    ?? document.QuerySelector("//h1");
                if (titleElement != null)
                {
                    chapterData.Title = titleElement.TextContent?.Trim() ?? "";
                }
            }

            return chapterData;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region IPageCrawler Implementation

    public async Task<CrawlerListResult<string>> CrawlPageUrlsAsync(
        string chapterUrl,
        CrawlerContext context)
    {
        try
        {
            Log(context, $"Bắt đầu crawl page URLs từ: {chapterUrl}", Application.Common.Models.LogLevel.Info);

            var chapterResult = await CrawlChapterAsync(chapterUrl, context);
            if (!chapterResult.IsSuccess || chapterResult.Data == null)
            {
                return CrawlerListResult<string>.Failure(
                    "Không thể crawl chapter để lấy page URLs",
                    chapterResult.Exception,
                    chapterUrl);
            }

            var pageUrls = chapterResult.Data.PageUrls.ToList();

            Log(context, $"Tìm thấy {pageUrls.Count} page URLs", Application.Common.Models.LogLevel.Info);

            return CrawlerListResult<string>.Success(pageUrls, pageUrls.Count, chapterUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi crawl page URLs: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerListResult<string>.Failure(
                $"Lỗi khi crawl page URLs: {ex.Message}",
                ex,
                chapterUrl);
        }
    }

    public async Task<CrawlerResult<byte[]>> DownloadPageAsync(
        string imageUrl,
        CrawlerContext context)
    {
        try
        {
            Log(context, $"Downloading image: {imageUrl}", Application.Common.Models.LogLevel.Debug);

            var fullUrl = BuildFullUrl(context.Source.BaseUrl, imageUrl);
            var imageData = await DownloadBytesAsync(fullUrl, context.CancellationToken);

            return CrawlerResult<byte[]>.Success(imageData, fullUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi download image: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerResult<byte[]>.Failure(
                $"Lỗi khi download image: {ex.Message}",
                ex,
                imageUrl);
        }
    }

    public async Task<CrawlerListResult<byte[]>> DownloadPagesAsync(
        IEnumerable<string> imageUrls,
        CrawlerContext context,
        int? maxPages = null)
    {
        try
        {
            var urls = imageUrls.ToList();
            var max = maxPages ?? urls.Count;
            var results = new List<byte[]>();

            Log(context, $"Bắt đầu download {max} pages", Application.Common.Models.LogLevel.Info);

            for (int i = 0; i < Math.Min(max, urls.Count); i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var url = urls[i];
                var result = await DownloadPageAsync(url, context);

                if (result.IsSuccess && result.Data != null)
                {
                    results.Add(result.Data);
                    UpdateProgress(context, i + 1, max);
                }
            }

            Log(context, $"Hoàn thành download {results.Count} pages", Application.Common.Models.LogLevel.Info);

            return CrawlerListResult<byte[]>.Success(results, results.Count, context.StartUrl);
        }
        catch (Exception ex)
        {
            Log(context, $"Lỗi khi download pages: {ex.Message}", Application.Common.Models.LogLevel.Error);
            return CrawlerListResult<byte[]>.Failure(
                $"Lỗi khi download pages: {ex.Message}",
                ex,
                context.StartUrl);
        }
    }

    #endregion
}


