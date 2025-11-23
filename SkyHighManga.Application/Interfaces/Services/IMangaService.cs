using SkyHighManga.Application.Common.Models;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Services;

/// <summary>
/// Service để lưu manga data vào database
/// </summary>
public interface IMangaService
{
    /// <summary>
    /// Lưu hoặc cập nhật manga từ crawl data
    /// </summary>
    Task<Manga> SaveOrUpdateMangaAsync(
        MangaCrawlData crawlData,
        Guid sourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu hoặc cập nhật chapter từ crawl data
    /// </summary>
    Task<Chapter> SaveOrUpdateChapterAsync(
        ChapterCrawlData crawlData,
        Guid mangaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu pages cho một chapter
    /// </summary>
    Task<int> SavePagesAsync(
        Guid chapterId,
        IEnumerable<string> pageUrls,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra manga đã tồn tại
    /// </summary>
    Task<bool> MangaExistsAsync(Guid sourceId, string sourceMangaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra chapter đã tồn tại
    /// </summary>
    Task<bool> ChapterExistsAsync(Guid mangaId, string sourceChapterId, CancellationToken cancellationToken = default);
}

