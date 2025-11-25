using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Repository cho CrawlJob
/// </summary>
public interface ICrawlJobRepository : IRepository<CrawlJob>
{
    /// <summary>
    /// Lấy job theo status
    /// </summary>
    Task<IEnumerable<CrawlJob>> GetByStatusAsync(CrawlJobStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy job theo source
    /// </summary>
    Task<IEnumerable<CrawlJob>> GetBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy job đang chạy
    /// </summary>
    Task<IEnumerable<CrawlJob>> GetRunningJobsAsync(CancellationToken cancellationToken = default);
}

