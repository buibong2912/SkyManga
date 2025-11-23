using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class MangaGenreConfiguration : IEntityTypeConfiguration<MangaGenre>
{
    public void Configure(EntityTypeBuilder<MangaGenre> builder)
    {
        builder.ToTable("MangaGenres");

        // Primary Key
        builder.HasKey(mg => mg.Id);

        // Properties
        builder.Property(mg => mg.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(mg => mg.Manga)
            .WithMany(m => m.MangaGenres)
            .HasForeignKey(mg => mg.MangaId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(mg => mg.Genre)
            .WithMany(g => g.MangaGenres)
            .HasForeignKey(mg => mg.GenreId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Indexes - Composite unique index to prevent duplicate manga-genre pairs
        builder.HasIndex(mg => new { mg.MangaId, mg.GenreId })
            .IsUnique();
        builder.HasIndex(mg => mg.MangaId);
        builder.HasIndex(mg => mg.GenreId);
    }
}

