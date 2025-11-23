using Microsoft.EntityFrameworkCore.Storage;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Mangas = new MangaRepository(context);
        Chapters = new ChapterRepository(context);
        Pages = new PageRepository(context);
        Authors = new AuthorRepository(context);
        Genres = new GenreRepository(context);
    }

    public IMangaRepository Mangas { get; }
    public IChapterRepository Chapters { get; }
    public IPageRepository Pages { get; }
    public IAuthorRepository Authors { get; }
    public IGenreRepository Genres { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

