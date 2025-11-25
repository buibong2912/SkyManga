using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Application.Interfaces.Repositories;

/// <summary>
/// Repository cho Author entity
/// </summary>
public interface IAuthorRepository : IRepository<Author>
{
    /// <summary>
    /// Tìm author theo tên
    /// </summary>
    Task<Author?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}


