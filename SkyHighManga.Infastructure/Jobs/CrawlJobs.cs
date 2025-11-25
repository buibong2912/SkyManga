using Hangfire;
using SkyHighManga.Infastructure.Services;

namespace SkyHighManga.Infastructure.Jobs;

/// <summary>
/// Hangfire jobs để trigger crawl
/// </summary>
public class CrawlJobs
{
    private readonly ICrawlJobOrchestratorService _orchestratorService;

    public CrawlJobs(ICrawlJobOrchestratorService orchestratorService)
    {
        _orchestratorService = orchestratorService;
    }

    /// <summary>
    /// Job để crawl tất cả mangas từ một source
    /// Có thể schedule hoặc trigger manually
    /// Hangfire sẽ tự động inject dependencies
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task CrawlAllMangasJob(
        Guid sourceId,
        int? maxPages = null)
    {
        await _orchestratorService.StartCrawlAllMangasAsync(sourceId, maxPages, CancellationToken.None);
    }

    /// <summary>
    /// Schedule recurring job để crawl tự động
    /// </summary>
    public static void ScheduleRecurringCrawl(
        string jobId,
        Guid sourceId,
        string cronExpression,
        int? maxPages = null)
    {
        // Sử dụng Expression<Func<CrawlJobs, Task>> để Hangfire có thể inject dependencies
        System.Linq.Expressions.Expression<Func<CrawlJobs, Task>> jobExpression = 
            x => x.CrawlAllMangasJob(sourceId, maxPages);
        
        RecurringJob.AddOrUpdate(
            jobId,
            jobExpression,
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });
    }
}

