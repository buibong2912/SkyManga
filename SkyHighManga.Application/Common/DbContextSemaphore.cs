using System.Threading;

namespace SkyHighManga.Application.Common
{
    public static class DbContextSemaphore
    {
        public static SemaphoreSlim Instance { get; } = new SemaphoreSlim(1, 1);
    }
}

