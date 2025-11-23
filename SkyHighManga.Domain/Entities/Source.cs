namespace SkyHighManga.Domain.Entities;

public class Source
{
    /// <summary>
    /// ID duy nhất của nguồn craw trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tên nguồn craw (ví dụ: "Nettruyen", "TruyenQQ") - tên hiển thị của website nguồn
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL gốc của website nguồn (ví dụ: "https://nettruyen.com") - dùng để build full URL khi crawl
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả về nguồn craw - thông tin bổ sung về website nguồn
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Loại nguồn (Website, API, RSS) - xác định cách thức crawl
    /// </summary>
    public SourceType Type { get; set; }
    
    /// <summary>
    /// Flag đánh dấu nguồn có đang hoạt động hay không - tắt nguồn nếu website down hoặc không crawl được
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Crawler Configuration
    /// <summary>
    /// Tên class crawler để sử dụng (ví dụ: "NettruyenCrawler") - dùng để load đúng crawler class
    /// </summary>
    public string? CrawlerClassName { get; set; }
    
    /// <summary>
    /// JSON config cho crawler - lưu các selector CSS, XPath, regex pattern để parse HTML
    /// </summary>
    public string? ConfigurationJson { get; set; }
    
    // Rate Limiting
    /// <summary>
    /// Số request tối đa mỗi phút - giới hạn để tránh bị block
    /// </summary>
    public int? RequestsPerMinute { get; set; }
    
    /// <summary>
    /// Số request tối đa mỗi giờ - giới hạn để tránh bị block
    /// </summary>
    public int? RequestsPerHour { get; set; }
    
    /// <summary>
    /// Độ trễ giữa các request tính bằng milliseconds - delay để tránh spam
    /// </summary>
    public int? DelayBetweenRequestsMs { get; set; }
    
    // Navigation Properties
    public ICollection<Manga> Mangas { get; set; } = new List<Manga>();
    public ICollection<CrawlJob> CrawlJobs { get; set; } = new List<CrawlJob>();
    
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
    /// Thời gian crawl nguồn lần cuối - dùng để biết khi nào cần crawl lại
    /// </summary>
    public DateTime? LastCrawledAt { get; set; }
}

public enum SourceType
{
    /// <summary>
    /// Website - crawl từ HTML của website
    /// </summary>
    Website = 1,
    
    /// <summary>
    /// API - crawl từ REST API
    /// </summary>
    API = 2,
    
    /// <summary>
    /// RSS - crawl từ RSS feed
    /// </summary>
    RSS = 3,
    
    /// <summary>
    /// Khác - các nguồn khác
    /// </summary>
    Other = 99
}

