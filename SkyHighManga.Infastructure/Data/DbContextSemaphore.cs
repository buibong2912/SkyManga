namespace SkyHighManga.Infastructure.Data;

/// <summary>
/// Shared semaphore để serialize tất cả database operations trong toàn bộ application
/// </summary>
public static class DbContextSemaphore
{
    private static readonly SemaphoreSlim _instance = new SemaphoreSlim(1, 1);
    
    public static SemaphoreSlim Instance => _instance;
}

