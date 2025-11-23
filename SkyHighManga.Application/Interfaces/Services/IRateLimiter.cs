namespace SkyHighManga.Application.Interfaces.Services;

/// <summary>
/// Service để giới hạn rate của requests
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Chờ nếu cần thiết để tuân thủ rate limit
    /// </summary>
    Task WaitIfNeededAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Đăng ký một request đã thực hiện
    /// </summary>
    void RegisterRequest();

    /// <summary>
    /// Reset rate limiter
    /// </summary>
    void Reset();
}

