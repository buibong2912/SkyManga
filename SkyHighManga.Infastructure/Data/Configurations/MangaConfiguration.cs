using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class MangaConfiguration : IEntityTypeConfiguration<Manga>
{
    public void Configure(EntityTypeBuilder<Manga> builder)
    {
        builder.ToTable("Mangas");

        // Primary Key
        builder.HasKey(m => m.Id);

        // Properties
        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.AlternativeTitle)
            .HasMaxLength(500);

        builder.Property(m => m.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.CoverImageUrl)
            .HasMaxLength(2000);

        builder.Property(m => m.ThumbnailUrl)
            .HasMaxLength(2000);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.OriginalLanguage)
            .HasMaxLength(50);

        builder.Property(m => m.ViewCount)
            .HasDefaultValue(0);

        builder.Property(m => m.RatingCount)
            .HasDefaultValue(0);

        builder.Property(m => m.SourceMangaId)
            .HasMaxLength(200);

        builder.Property(m => m.SourceUrl)
            .HasMaxLength(2000);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(m => m.Author)
            .WithMany(a => a.Mangas)
            .HasForeignKey(m => m.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Source)
            .WithMany(s => s.Mangas)
            .HasForeignKey(m => m.SourceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.MangaGenres)
            .WithOne(mg => mg.Manga)
            .HasForeignKey(mg => mg.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => m.Title);
        builder.HasIndex(m => m.SourceId);
        builder.HasIndex(m => m.AuthorId);
        builder.HasIndex(m => m.SourceMangaId);
        builder.HasIndex(m => new { m.SourceId, m.SourceMangaId })
            .IsUnique()
            .HasFilter("[SourceMangaId] IS NOT NULL");
        builder.HasIndex(m => m.IsActive);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.CreatedAt);
    }
}

