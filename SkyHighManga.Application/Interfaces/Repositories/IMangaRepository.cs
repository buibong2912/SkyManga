using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Repository cho Manga entity
/// </summary>
public interface IMangaRepository : IRepository<Manga>
{
    /// <summary>
    /// Tìm manga theo SourceMangaId và SourceId
    /// </summary>
    Task<Manga?> FindBySourceIdAsync(Guid sourceId, string sourceMangaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm manga theo SourceUrl
    /// </summary>
    Task<Manga?> FindBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra manga đã tồn tại theo SourceMangaId
    /// </summary>
    Task<bool> ExistsBySourceIdAsync(Guid sourceId, string sourceMangaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách manga theo SourceId
    /// </summary>
    Task<IEnumerable<Manga>> GetBySourceIdAsync(Guid sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy manga với chapters và pages
    /// </summary>
    Task<Manga?> GetWithChaptersAsync(Guid mangaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy manga với đầy đủ relationships
    /// </summary>
    Task<Manga?> GetFullAsync(Guid mangaId, CancellationToken cancellationToken = default);
}

