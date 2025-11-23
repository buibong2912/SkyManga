namespace SkyHighManga.Application.Interfaces.Services;

/// <summary>
/// Service để download images
/// </summary>
public interface IImageDownloader
{
    /// <summary>
    /// Download image từ URL
    /// </summary>
    Task<byte[]> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download image và lưu vào file
    /// </summary>
    Task<string> DownloadImageToFileAsync(
        string imageUrl,
        string savePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download nhiều images
    /// </summary>
    Task<Dictionary<string, byte[]>> DownloadImagesAsync(
        IEnumerable<string> imageUrls,
        CancellationToken cancellationToken = default,
        int? maxConcurrent = null);

    /// <summary>
    /// Validate image format
    /// </summary>
    bool IsValidImage(byte[] imageData);

    /// <summary>
    /// Get image format từ data
    /// </summary>
    string? GetImageFormat(byte[] imageData);

    /// <summary>
    /// Get image dimensions
    /// </summary>
    (int width, int height)? GetImageDimensions(byte[] imageData);
}

