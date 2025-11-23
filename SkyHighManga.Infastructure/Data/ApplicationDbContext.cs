using Microsoft.EntityFrameworkCore;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Manga> Mangas { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<MangaGenre> MangaGenres { get; set; }
    public DbSet<Source> Sources { get; set; }
    public DbSet<CrawlJob> CrawlJobs { get; set; }
    public DbSet<CrawlJobLog> CrawlJobLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

