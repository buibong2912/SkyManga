namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern để quản lý transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IMangaRepository Mangas { get; }
    IChapterRepository Chapters { get; }
    IPageRepository Pages { get; }
    IAuthorRepository Authors { get; }
    IGenreRepository Genres { get; }

    /// <summary>
    /// Lưu tất cả thay đổi
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bắt đầu transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

