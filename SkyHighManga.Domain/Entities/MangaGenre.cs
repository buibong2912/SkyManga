namespace SkyHighManga.Domain.Entities;

public class MangaGenre
{
    /// <summary>
    /// ID duy nhất của quan hệ manga-genre
    /// </summary>
    public Guid Id { get; set; }
    
    // Foreign Keys
    /// <summary>
    /// ID truyện - liên kết với bảng Manga
    /// </summary>
    public Guid MangaId { get; set; }
    
    /// <summary>
    /// ID thể loại - liên kết với bảng Genre
    /// </summary>
    public Guid GenreId { get; set; }
    
    // Navigation Properties
    public Manga Manga { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
    
    // Metadata
    /// <summary>
    /// Thời gian tạo record - lưu khi crawl và gán thể loại cho truyện
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

