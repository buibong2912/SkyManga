namespace SkyHighManga.Domain.Entities;

public class Manga
{
    /// <summary>
    /// ID duy nhất của truyện trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tên truyện chính - lưu từ title tag hoặc h1 trên trang nguồn
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên truyện thay thế/khác - lưu từ alternative title trên trang nguồn
    /// </summary>
    public string? AlternativeTitle { get; set; }
    
    /// <summary>
    /// Mô tả/tóm tắt nội dung truyện - lưu từ description/summary trên trang nguồn
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL ảnh bìa truyện - lưu từ cover image trên trang nguồn
    /// </summary>
    public string? CoverImageUrl { get; set; }
    
    /// <summary>
    /// URL ảnh thumbnail nhỏ hơn - lưu từ thumbnail image trên trang nguồn
    /// </summary>
    public string? ThumbnailUrl { get; set; }
    
    /// <summary>
    /// Trạng thái truyện (Đang ra, Hoàn thành, Tạm dừng, Hủy) - lưu từ status trên trang nguồn
    /// </summary>
    public MangaStatus Status { get; set; }
    
    /// <summary>
    /// Năm phát hành - lưu từ year of release trên trang nguồn
    /// </summary>
    public int? YearOfRelease { get; set; }
    
    /// <summary>
    /// Ngôn ngữ gốc (Tiếng Nhật, Tiếng Hàn, etc.) - lưu từ original language trên trang nguồn
    /// </summary>
    public string? OriginalLanguage { get; set; }
    
    /// <summary>
    /// Số lượt xem - lưu từ view count trên trang nguồn
    /// </summary>
    public int ViewCount { get; set; }
    
    /// <summary>
    /// Điểm đánh giá trung bình - lưu từ rating trên trang nguồn
    /// </summary>
    public double? Rating { get; set; }
    
    /// <summary>
    /// Số lượng người đánh giá - lưu từ rating count trên trang nguồn
    /// </summary>
    public int RatingCount { get; set; }
    
    // Foreign Keys
    /// <summary>
    /// ID tác giả - liên kết với bảng Author
    /// </summary>
    public Guid? AuthorId { get; set; }
    
    /// <summary>
    /// ID nguồn craw - liên kết với bảng Source để biết truyện từ nguồn nào
    /// </summary>
    public Guid SourceId { get; set; }
    
    // Navigation Properties
    public Author? Author { get; set; }
    
    public Source Source { get; set; } = null!;
    
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    
    public ICollection<MangaGenre> MangaGenres { get; set; } = new List<MangaGenre>();
    
    // Metadata
    /// <summary>
    /// ID truyện từ nguồn craw (ví dụ: "12345" từ nguồn) - dùng để check duplicate và update
    /// </summary>
    public string? SourceMangaId { get; set; }
    
    /// <summary>
    /// URL gốc của trang truyện trên nguồn - lưu để có thể quay lại crawl update
    /// </summary>
    public string? SourceUrl { get; set; }
    
    /// <summary>
    /// Thời gian tạo record trong database
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật record lần cuối
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Thời gian crawl/update truyện lần cuối - dùng để biết khi nào cần crawl lại
    /// </summary>
    public DateTime? LastCrawledAt { get; set; }
    
    /// <summary>
    /// Flag đánh dấu truyện còn hoạt động hay đã xóa (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

public enum MangaStatus
{
    /// <summary>
    /// Đang ra - truyện vẫn đang được cập nhật
    /// </summary>
    Ongoing = 1,
    
    /// <summary>
    /// Hoàn thành - truyện đã kết thúc
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Tạm dừng - truyện tạm thời không ra chương mới
    /// </summary>
    OnHold = 3,
    
    /// <summary>
    /// Hủy - truyện đã bị hủy
    /// </summary>
    Cancelled = 4,
    
    /// <summary>
    /// Không xác định - không rõ trạng thái
    /// </summary>
    Unknown = 0
}

