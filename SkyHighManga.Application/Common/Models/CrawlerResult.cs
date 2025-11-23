using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Common.Models;

/// <summary>
/// Kết quả của một lần crawl
/// </summary>
public class CrawlerResult<T>
{
    /// <summary>
    /// Thành công hay không
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Dữ liệu đã crawl được
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Thông báo lỗi nếu có
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception nếu có lỗi
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// URL đã crawl
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Thời gian crawl
    /// </summary>
    public DateTime CrawledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời gian xử lý (milliseconds)
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    public static CrawlerResult<T> Success(T data, string? url = null)
    {
        return new CrawlerResult<T>
        {
            IsSuccess = true,
            Data = data,
            Url = url
        };
    }

    public static CrawlerResult<T> Failure(string errorMessage, Exception? exception = null, string? url = null)
    {
        return new CrawlerResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Url = url
        };
    }
}

/// <summary>
/// Kết quả crawl danh sách
/// </summary>
public class CrawlerListResult<T> : CrawlerResult<IEnumerable<T>>
{
    /// <summary>
    /// Tổng số items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Số items thành công
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Số items thất bại
    /// </summary>
    public int FailedCount { get; set; }

    public static CrawlerListResult<T> Success(IEnumerable<T> data, int totalCount, string? url = null)
    {
        var items = data.ToList();
        return new CrawlerListResult<T>
        {
            IsSuccess = true,
            Data = items,
            TotalCount = totalCount,
            SuccessCount = items.Count,
            FailedCount = totalCount - items.Count,
            Url = url
        };
    }

    public static new CrawlerListResult<T> Failure(string errorMessage, Exception? exception = null, string? url = null)
    {
        return new CrawlerListResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            Url = url,
            TotalCount = 0,
            SuccessCount = 0,
            FailedCount = 0
        };
    }
}

