namespace SkyHighManga.Domain.Entities;

public class CrawlJob
{
    /// <summary>
    /// ID duy nhất của job crawl trong hệ thống
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tên job crawl (ví dụ: "Crawl Nettruyen - Full", "Update One Piece") - tên mô tả job
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả chi tiết về job crawl - thông tin bổ sung về mục đích job
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Loại job crawl (FullCrawl, SingleManga, UpdateManga, etc.) - xác định cách thức crawl
    /// </summary>
    public CrawlJobType Type { get; set; }
    
    /// <summary>
    /// Trạng thái job (Pending, Running, Completed, Failed, etc.) - track trạng thái hiện tại
    /// </summary>
    public CrawlJobStatus Status { get; set; }
    
    // Foreign Keys
    /// <summary>
    /// ID nguồn craw - liên kết với bảng Source, xác định crawl từ nguồn nào
    /// </summary>
    public Guid SourceId { get; set; }
    
    /// <summary>
    /// ID truyện (nếu crawl một manga cụ thể) - liên kết với bảng Manga
    /// </summary>
    public Guid? MangaId { get; set; }
    
    // Navigation Properties
    public Source Source { get; set; } = null!;
    public Manga? Manga { get; set; }
    public ICollection<CrawlJobLog> Logs { get; set; } = new List<CrawlJobLog>();
    
    // Configuration
    /// <summary>
    /// JSON config cho job - lưu các tham số cấu hình đặc biệt cho job này
    /// </summary>
    public string? ConfigurationJson { get; set; }
    
    /// <summary>
    /// URL bắt đầu crawl - trang đầu tiên để bắt đầu crawl (ví dụ: trang danh sách truyện)
    /// </summary>
    public string? StartUrl { get; set; }
    
    /// <summary>
    /// Giới hạn số trang tối đa crawl - dùng để test hoặc giới hạn crawl
    /// </summary>
    public int? MaxPages { get; set; }
    
    /// <summary>
    /// Giới hạn số chapter tối đa crawl - dùng để test hoặc giới hạn crawl
    /// </summary>
    public int? MaxChapters { get; set; }
    
    // Progress Tracking
    /// <summary>
    /// Tổng số items cần crawl (truyện/chapter/pages) - tính toán trước khi bắt đầu
    /// </summary>
    public int TotalItems { get; set; }
    
    /// <summary>
    /// Số items đã xử lý - tăng dần khi crawl từng item
    /// </summary>
    public int ProcessedItems { get; set; }
    
    /// <summary>
    /// Số items crawl thành công - tăng khi crawl item thành công
    /// </summary>
    public int SuccessItems { get; set; }
    
    /// <summary>
    /// Số items crawl thất bại - tăng khi crawl item bị lỗi
    /// </summary>
    public int FailedItems { get; set; }
    
    // Timing
    /// <summary>
    /// Thời gian lên lịch chạy job - dùng cho scheduled jobs
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
    
    /// <summary>
    /// Thời gian bắt đầu chạy job - lưu khi job chuyển sang status Running
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// Thời gian hoàn thành job - lưu khi job chuyển sang status Completed/Failed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Thời gian chạy job - tính từ StartedAt đến CompletedAt
    /// </summary>
    public TimeSpan? Duration { get; set; }
    
    // Error Handling
    /// <summary>
    /// Thông báo lỗi nếu job thất bại - lưu error message khi job fail
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Stack trace của exception nếu job thất bại - lưu để debug
    /// </summary>
    public string? StackTrace { get; set; }
    
    // Metadata
    /// <summary>
    /// Thời gian tạo record trong database
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thời gian cập nhật record lần cuối - update liên tục khi job đang chạy
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Người tạo job (username hoặc system) - lưu ai tạo job này
    /// </summary>
    public string? CreatedBy { get; set; }
}

public enum CrawlJobType
{
    /// <summary>
    /// Crawl toàn bộ source - crawl tất cả truyện từ nguồn
    /// </summary>
    FullCrawl = 1,
    
    /// <summary>
    /// Crawl một manga cụ thể - chỉ crawl một truyện được chỉ định
    /// </summary>
    SingleManga = 2,
    
    /// <summary>
    /// Update manga đã có - chỉ crawl các chapter mới của truyện đã có
    /// </summary>
    UpdateManga = 3,
    
    /// <summary>
    /// Tìm kiếm và crawl - tìm kiếm truyện theo keyword rồi crawl
    /// </summary>
    SearchAndCrawl = 4,
    
    /// <summary>
    /// Update theo lịch - job tự động chạy định kỳ để update
    /// </summary>
    ScheduledUpdate = 5
}

public enum CrawlJobStatus
{
    /// <summary>
    /// Đang chờ - job đã tạo nhưng chưa chạy
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Đang chạy - job đang được thực thi
    /// </summary>
    Running = 2,
    
    /// <summary>
    /// Hoàn thành - job đã chạy xong thành công
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Thất bại - job bị lỗi và dừng lại
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Đã hủy - job bị hủy bởi user hoặc system
    /// </summary>
    Cancelled = 5,
    
    /// <summary>
    /// Tạm dừng - job tạm thời bị pause
    /// </summary>
    Paused = 6
}

