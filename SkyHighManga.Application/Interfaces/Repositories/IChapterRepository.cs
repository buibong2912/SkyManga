using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Repository cho Chapter entity
/// </summary>
public interface IChapterRepository : IRepository<Chapter>
{
    /// <summary>
    /// Tìm chapter theo SourceChapterId và MangaId
    /// </summary>
    Task<Chapter?> FindBySourceIdAsync(Guid mangaId, string sourceChapterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm chapter theo SourceUrl
    /// </summary>
    Task<Chapter?> FindBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra chapter đã tồn tại
    /// </summary>
    Task<bool> ExistsBySourceIdAsync(Guid mangaId, string sourceChapterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách SourceChapterIds đã tồn tại (batch check để tối ưu)
    /// </summary>
    Task<HashSet<string>> GetExistingSourceChapterIdsAsync(Guid mangaId, IEnumerable<string> sourceChapterIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách chapters của một manga
    /// </summary>
    Task<IEnumerable<Chapter>> GetByMangaIdAsync(Guid mangaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chapters mới nhất của một manga (chưa crawl)
    /// </summary>
    Task<IEnumerable<Chapter>> GetNewChaptersAsync(Guid mangaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chapter với pages
    /// </summary>
    Task<Chapter?> GetWithPagesAsync(Guid chapterId, CancellationToken cancellationToken = default);
}


