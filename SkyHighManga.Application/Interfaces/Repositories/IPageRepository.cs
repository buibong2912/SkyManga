using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Repository cho Page entity
/// </summary>
public interface IPageRepository : IRepository<Page>
{
    /// <summary>
    /// Tìm page theo SourcePageId và ChapterId
    /// </summary>
    Task<Page?> FindBySourceIdAsync(Guid chapterId, string sourcePageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra page đã tồn tại
    /// </summary>
    Task<bool> ExistsBySourceIdAsync(Guid chapterId, string sourcePageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách pages của một chapter
    /// </summary>
    Task<IEnumerable<Page>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy pages chưa download
    /// </summary>
    Task<IEnumerable<Page>> GetNotDownloadedAsync(Guid chapterId, CancellationToken cancellationToken = default);
}

