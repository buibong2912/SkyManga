using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Repositories;

public class PageRepository : Repository<Page>, IPageRepository
{
    public PageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Page?> FindBySourceIdAsync(Guid chapterId, string sourcePageId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ChapterId == chapterId && p.SourcePageId == sourcePageId, cancellationToken);
    }

    public async Task<bool> ExistsBySourceIdAsync(Guid chapterId, string sourcePageId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(p => p.ChapterId == chapterId && p.SourcePageId == sourcePageId, cancellationToken);
    }

    public async Task<IEnumerable<Page>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ChapterId == chapterId)
            .OrderBy(p => p.PageNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Page>> GetNotDownloadedAsync(Guid chapterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ChapterId == chapterId && !p.IsDownloaded)
            .OrderBy(p => p.PageNumber)
            .ToListAsync(cancellationToken);
    }
}

