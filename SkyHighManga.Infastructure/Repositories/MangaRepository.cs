using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Repositories;

public class MangaRepository : Repository<Manga>, IMangaRepository
{
    public MangaRepository(ApplicationDbContext context) : base(context)
    {
    }

    public IQueryable<Manga> GetAll()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<Manga?> FindBySourceIdAsync(Guid sourceId, string sourceMangaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.SourceId == sourceId && m.SourceMangaId == sourceMangaId, cancellationToken);
    }

    public async Task<Manga?> FindBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.SourceUrl == sourceUrl, cancellationToken);
    }

    public async Task<bool> ExistsBySourceIdAsync(Guid sourceId, string sourceMangaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(m => m.SourceId == sourceId && m.SourceMangaId == sourceMangaId, cancellationToken);
    }

    public async Task<IEnumerable<Manga>> GetBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.SourceId == sourceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Manga?> GetWithChaptersAsync(Guid mangaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Chapters)
            .FirstOrDefaultAsync(m => m.Id == mangaId, cancellationToken);
    }

    public async Task<Manga?> GetFullAsync(Guid mangaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Author)
            .Include(m => m.Source)
            .Include(m => m.MangaGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.Chapters)
                .ThenInclude(c => c.Pages)
            .FirstOrDefaultAsync(m => m.Id == mangaId, cancellationToken);
    }
}

