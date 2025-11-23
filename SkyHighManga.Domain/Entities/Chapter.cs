namespace SkyHighManga.Domain.Entities;

public class Chapter
{
    /// <summary>
    /// ID duy nhất của chương trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tên chương - lưu từ title của chương trên trang nguồn
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Số chương (có thể là số hoặc chuỗi như "1.5", "Extra", "Chapter 1") - lưu từ chapter number trên trang nguồn
    /// </summary>
    public string? ChapterNumber { get; set; }
    
    /// <summary>
    /// Thứ tự sắp xếp chương (1, 2, 3...) - dùng để sort, lưu từ thứ tự trên trang nguồn
    /// </summary>
    public int? ChapterIndex { get; set; }
    
    /// <summary>
    /// Số lượng trang trong chương - lưu từ số lượng images/pages trên trang nguồn
    /// </summary>
    public int PageCount { get; set; }
    
    // Foreign Keys
    /// <summary>
    /// ID truyện - liên kết với bảng Manga
    /// </summary>
    public Guid MangaId { get; set; }
    
    // Navigation Properties
    public Manga Manga { get; set; } = null!;
    public ICollection<Page> Pages { get; set; } = new List<Page>();
    
    // Metadata
    /// <summary>
    /// ID chương từ nguồn craw (ví dụ: "ch123") - dùng để check duplicate và update
    /// </summary>
    public string? SourceChapterId { get; set; }
    
    /// <summary>
    /// URL gốc của trang chương trên nguồn - lưu để có thể quay lại crawl update
    /// </summary>
    public string? SourceUrl { get; set; }
    
    /// <summary>
    /// Count View Chapter
    /// </summary>
    public int CountView { get; set; }
    
    /// <summary>
    /// Thời gian tạo record trong database
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật record lần cuối
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Thời gian xuất bản chương - lưu từ published date trên trang nguồn
    /// </summary>
    public DateTime? PublishedAt { get; set; }
    
    /// <summary>
    /// Flag đánh dấu chương còn hoạt động hay đã xóa (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

