namespace SkyHighManga.Application.Common.Models;

/// <summary>
/// Dữ liệu manga đã crawl được
/// </summary>
public class MangaCrawlData
{
    /// <summary>
    /// ID từ nguồn
    /// </summary>
    public string? SourceMangaId { get; set; }

    /// <summary>
    /// URL gốc
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tên truyện
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Tên thay thế
    /// </summary>
    public string? AlternativeTitle { get; set; }

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL ảnh bìa
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// URL thumbnail
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Năm phát hành
    /// </summary>
    public int? YearOfRelease { get; set; }

    /// <summary>
    /// Ngôn ngữ gốc
    /// </summary>
    public string? OriginalLanguage { get; set; }

    /// <summary>
    /// Số lượt xem
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Điểm đánh giá
    /// </summary>
    public double? Rating { get; set; }

    /// <summary>
    /// Số lượng đánh giá
    /// </summary>
    public int RatingCount { get; set; }

    /// <summary>
    /// Tên tác giả
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Danh sách thể loại
    /// </summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Danh sách chapters
    /// </summary>
    public List<ChapterCrawlData> Chapters { get; set; } = new();
}

/// <summary>
/// Dữ liệu chapter đã crawl được
/// </summary>
public class ChapterCrawlData
{
    /// <summary>
    /// ID từ nguồn
    /// </summary>
    public string? SourceChapterId { get; set; }

    /// <summary>
    /// URL gốc
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Tên chương
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Số chương
    /// </summary>
    public string? ChapterNumber { get; set; }

    /// <summary>
    /// Thứ tự chương
    /// </summary>
    public int? ChapterIndex { get; set; }

    /// <summary>
    /// Thời gian xuất bản
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Danh sách URLs của các trang
    /// </summary>
    public List<string> PageUrls { get; set; } = new();
}

