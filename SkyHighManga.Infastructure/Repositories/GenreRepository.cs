using Microsoft.EntityFrameworkCore;
using SkyHighManga.Application.Interfaces.Repositories;
using SkyHighManga.Domain.Entities;
using SkyHighManga.Infastructure.Data;

namespace SkyHighManga.Infastructure.Repositories;

public class GenreRepository : Repository<Genre>, IGenreRepository
{
    public GenreRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Genre?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.Name == name, cancellationToken);
    }

    public async Task<Genre> FindOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var genre = await FindByNameAsync(name, cancellationToken);
        if (genre == null)
        {
            genre = new Genre
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await AddAsync(genre, cancellationToken);
        }
        return genre;
    }
}

