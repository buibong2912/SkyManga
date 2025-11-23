namespace SkyHighManga.Domain.Entities;

public class Genre
{
    /// <summary>
    /// ID duy nhất của thể loại trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tên thể loại (Action, Romance, Comedy, etc.) - lưu từ genre name trên trang nguồn
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả về thể loại - lưu từ description trên trang nguồn (nếu có)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Slug URL-friendly (action, romance, comedy) - dùng để tạo URL, lưu từ slug hoặc tự generate từ Name
    /// </summary>
    public string? Slug { get; set; }
    
    // Navigation Properties
    public ICollection<MangaGenre> MangaGenres { get; set; } = new List<MangaGenre>();
    
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
    /// Flag đánh dấu thể loại còn hoạt động hay đã xóa (soft delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

