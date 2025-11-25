using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.ToTable("Chapters");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ChapterNumber)
            .HasMaxLength(50);

        builder.Property(c => c.PageCount)
            .HasDefaultValue(0);

        builder.Property(c => c.CountView)
            .HasDefaultValue(0);

        builder.Property(c => c.SourceChapterId)
            .HasMaxLength(200);

        builder.Property(c => c.SourceUrl)
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(c => c.Manga)
            .WithMany(m => m.Chapters)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(c => c.Pages)
            .WithOne(p => p.Chapter)
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.MangaId);
        builder.HasIndex(c => c.ChapterIndex);
        builder.HasIndex(c => c.SourceChapterId);
        builder.HasIndex(c => new { c.MangaId, c.ChapterIndex });
        builder.HasIndex(c => new { c.MangaId, c.SourceChapterId })
            .IsUnique()
            .HasFilter("\"SourceChapterId\" IS NOT NULL");
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.PublishedAt);
    }
}

