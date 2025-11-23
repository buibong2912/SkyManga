namespace SkyHighManga.Domain.Entities;

public class Author
{
    /// <summary>
    /// ID duy nhất của tác giả trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tên tác giả - lưu từ author name trên trang nguồn
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên tác giả thay thế/khác - lưu từ alternative name trên trang nguồn
    /// </summary>
    public string? AlternativeName { get; set; }
    
    /// <summary>
    /// Tiểu sử/giới thiệu về tác giả - lưu từ bio/description trên trang nguồn
    /// </summary>
    public string? Bio { get; set; }
    
    /// <summary>
    /// URL ảnh đại diện tác giả - lưu từ profile image trên trang nguồn
    /// </summary>
    public string? ProfileImageUrl { get; set; }
    
    // Navigation Properties
    public ICollection<Manga> Mangas { get; set; } = new List<Manga>();
    
    // Metadata
    /// <summary>
    /// Thời gian tạo record trong database
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật record lần cuối
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Flag đánh dấu tác giả còn hoạt động hay đã xóa (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

