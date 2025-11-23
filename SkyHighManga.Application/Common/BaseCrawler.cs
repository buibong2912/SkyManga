using SkyHighManga.Application.Common.Models;
using SkyHighManga.Application.Interfaces.Crawlers;
using SkyHighManga.Application.Interfaces.Services;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Common;

/// <summary>
/// Base class cho các crawlers, cung cấp common functionality
/// </summary>
public abstract class BaseCrawler : ICrawler
{
    protected readonly IRateLimiter? RateLimiter;
    protected readonly Func<HttpClient>? HttpClientFactory;

    protected BaseCrawler(
        IRateLimiter? rateLimiter = null,
        Func<HttpClient>? httpClientFactory = null)
    {
        RateLimiter = rateLimiter;
        HttpClientFactory = httpClientFactory;
    }

    public abstract string Name { get; }
    public abstract SourceType SupportedSourceType { get; }

    public virtual bool CanCrawl(Source source)
    {
        return source.Type == SupportedSourceType 
            && source.IsActive 
            && !string.IsNullOrEmpty(source.BaseUrl);
    }

    public virtual async Task<bool> TestConnectionAsync(Source source, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = CreateHttpClient();
            var response = await httpClient.GetAsync(source.BaseUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tạo HttpClient để sử dụng
    /// </summary>
    protected HttpClient CreateHttpClient()
    {
        if (HttpClientFactory != null)
        {
            return HttpClientFactory();
        }

        return new HttpClient();
    }

    /// <summary>
    /// Download HTML từ URL
    /// </summary>
    protected async Task<string> DownloadHtmlAsync(string url, CancellationToken cancellationToken = default)
    {
        await WaitForRateLimitAsync(cancellationToken);

        var httpClient = CreateHttpClient();
        var response = await httpClient.GetStringAsync(url, cancellationToken);
        
        RegisterRequest();
        
        return response;
    }

    /// <summary>
    /// Download bytes từ URL
    /// </summary>
    protected async Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken = default)
    {
        await WaitForRateLimitAsync(cancellationToken);

        var httpClient = CreateHttpClient();
        var response = await httpClient.GetByteArrayAsync(url, cancellationToken);
        
        RegisterRequest();
        
        return response;
    }

    /// <summary>
    /// Build full URL từ relative URL
    /// </summary>
    protected string BuildFullUrl(string baseUrl, string relativeUrl)
    {
        if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
        {
            return relativeUrl;
        }

        var baseUri = new Uri(baseUrl);
        return new Uri(baseUri, relativeUrl).ToString();
    }

    /// <summary>
    /// Log message
    /// </summary>
    protected void Log(CrawlerContext context, string message, Models.LogLevel level = Models.LogLevel.Info)
    {
        context.OnLog?.Invoke(message, level);
    }

    /// <summary>
    /// Update progress
    /// </summary>
    protected void UpdateProgress(CrawlerContext context, int processed, int total)
    {
        context.OnProgress?.Invoke(processed, total);
    }

    /// <summary>
    /// Chờ rate limit nếu cần
    /// </summary>
    protected async Task WaitForRateLimitAsync(CancellationToken cancellationToken = default)
    {
        if (RateLimiter != null)
        {
            await RateLimiter.WaitIfNeededAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Đăng ký request
    /// </summary>
    protected void RegisterRequest()
    {
        RateLimiter?.RegisterRequest();
    }
}

