using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Repositories;

public class ChapterRepository : Repository<Chapter>, IChapterRepository
{
    public ChapterRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Chapter?> FindBySourceIdAsync(Guid mangaId, string sourceChapterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.MangaId == mangaId && c.SourceChapterId == sourceChapterId, cancellationToken);
    }

    public async Task<Chapter?> FindBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.SourceUrl == sourceUrl, cancellationToken);
    }

    public async Task<bool> ExistsBySourceIdAsync(Guid mangaId, string sourceChapterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(c => c.MangaId == mangaId && c.SourceChapterId == sourceChapterId, cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingSourceChapterIdsAsync(Guid mangaId, IEnumerable<string> sourceChapterIds, CancellationToken cancellationToken = default)
    {
        var sourceChapterIdsList = sourceChapterIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        if (sourceChapterIdsList.Count == 0)
        {
            return new HashSet<string>();
        }

        // Sử dụng một query duy nhất để check nhiều chapters cùng lúc
        var existingIds = await _dbSet
            .Where(c => c.MangaId == mangaId && sourceChapterIdsList.Contains(c.SourceChapterId))
            .Select(c => c.SourceChapterId)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<Chapter>> GetByMangaIdAsync(Guid mangaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.MangaId == mangaId)
            .OrderBy(c => c.ChapterIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Chapter>> GetNewChaptersAsync(Guid mangaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.MangaId == mangaId && c.Pages.Count == 0)
            .OrderBy(c => c.ChapterIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<Chapter?> GetWithPagesAsync(Guid chapterId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Pages)
            .FirstOrDefaultAsync(c => c.Id == chapterId, cancellationToken);
    }
}

