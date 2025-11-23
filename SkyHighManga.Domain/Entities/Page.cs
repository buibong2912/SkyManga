namespace SkyHighManga.Domain.Entities;

public class Page
{
    /// <summary>
    /// ID duy nhất của trang trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Số thứ tự trang trong chương (1, 2, 3...) - lưu từ thứ tự image trên trang nguồn
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// URL ảnh gốc trên nguồn - lưu từ src của img tag trên trang nguồn
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Đường dẫn file ảnh đã download về máy (ví dụ: "/images/manga123/chapter1/page1.jpg") - lưu sau khi download thành công
    /// </summary>
    public string? LocalFilePath { get; set; }
    
    /// <summary>
    /// Kích thước file ảnh tính bằng bytes - lưu sau khi download để kiểm tra file size
    /// </summary>
    public long? FileSize { get; set; }
    
    /// <summary>
    /// Định dạng ảnh (jpg, png, webp, gif, etc.) - lưu từ extension hoặc content-type
    /// </summary>
    public string? ImageFormat { get; set; }
    
    /// <summary>
    /// Chiều rộng ảnh tính bằng pixels - lưu từ image metadata sau khi download
    /// </summary>
    public int? Width { get; set; }
    
    /// <summary>
    /// Chiều cao ảnh tính bằng pixels - lưu từ image metadata sau khi download
    /// </summary>
    public int? Height { get; set; }
    
    // Foreign Keys
    /// <summary>
    /// ID chương - liên kết với bảng Chapter
    /// </summary>
    public Guid ChapterId { get; set; }
    
    // Navigation Properties
    public Chapter Chapter { get; set; } = null!;
    
    // Metadata
    /// <summary>
    /// ID trang từ nguồn craw (nếu có) - dùng để check duplicate
    /// </summary>
    public string? SourcePageId { get; set; }
    
    /// <summary>
    /// Thời gian tạo record trong database
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật record lần cuối
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Flag đánh dấu ảnh đã được download về máy hay chưa
    /// </summary>
    public bool IsDownloaded { get; set; } = false;
    
    /// <summary>
    /// Flag đánh dấu trang còn hoạt động hay đã xóa (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

