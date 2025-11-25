using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Repositories;

public class CrawlJobRepository : Repository<CrawlJob>, ICrawlJobRepository
{
    public CrawlJobRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CrawlJob>> GetByStatusAsync(CrawlJobStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(j => j.Status == status)
            .Include(j => j.Source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CrawlJob>> GetBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(j => j.SourceId == sourceId)
            .Include(j => j.Source)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CrawlJob>> GetRunningJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(j => j.Status == CrawlJobStatus.Running)
            .Include(j => j.Source)
            .ToListAsync(cancellationToken);
    }
}

