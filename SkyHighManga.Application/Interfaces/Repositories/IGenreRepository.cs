using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Repository cho Genre entity
/// </summary>
public interface IGenreRepository : IRepository<Genre>
{
    /// <summary>
    /// Tìm genre theo tên
    /// </summary>
    Task<Genre?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm hoặc tạo genre theo tên
    /// </summary>
    Task<Genre> FindOrCreateAsync(string name, CancellationToken cancellationToken = default);
}

