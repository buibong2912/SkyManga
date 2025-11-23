namespace SkyHighManga.Domain.Entities;

public class CrawlJobLog
{
    /// <summary>
    /// ID duy nhất của log entry
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Nội dung log message - lưu thông tin về hành động đang thực hiện
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Mức độ log (Debug, Info, Warning, Error, Critical) - phân loại mức độ quan trọng
    /// </summary>
    public LogLevel Level { get; set; }
    
    /// <summary>
    /// Thông tin exception nếu có lỗi - lưu exception message khi có error
    /// </summary>
    public string? Exception { get; set; }
    
    /// <summary>
    /// Stack trace của exception - lưu để debug chi tiết
    /// </summary>
    public string? StackTrace { get; set; }
    
    // Foreign Keys
    /// <summary>
    /// ID job crawl - liên kết với bảng CrawlJob
    /// </summary>
    public Guid CrawlJobId { get; set; }
    
    // Navigation Properties
    public CrawlJob CrawlJob { get; set; } = null!;
    
    // Additional Data
    /// <summary>
    /// URL đang crawl khi tạo log - lưu để biết đang crawl URL nào khi có log
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// JSON data bổ sung - lưu thông tin bổ sung dạng JSON (request headers, response data, etc.)
    /// </summary>
    public string? AdditionalData { get; set; }
    
    // Metadata
    /// <summary>
    /// Thời gian tạo log - lưu khi nào log được tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

public enum LogLevel
{
    /// <summary>
    /// Debug - thông tin debug chi tiết
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// Info - thông tin bình thường về quá trình crawl
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Warning - cảnh báo nhưng không phải lỗi
    /// </summary>
    Warning = 3,
    
    /// <summary>
    /// Error - lỗi xảy ra nhưng job vẫn tiếp tục
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Critical - lỗi nghiêm trọng khiến job phải dừng
    /// </summary>
    Critical = 5
}

