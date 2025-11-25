using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("Sources");

        // Primary Key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Description)
            .HasColumnType("text");

        builder.Property(s => s.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CrawlerClassName)
            .HasMaxLength(200);

        builder.Property(s => s.ConfigurationJson)
            .HasColumnType("text");

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasMany(s => s.Mangas)
            .WithOne(m => m.Source)
            .HasForeignKey(m => m.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.CrawlJobs)
            .WithOne(cj => cj.Source)
            .HasForeignKey(cj => cj.SourceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.Name)
            .IsUnique();
        builder.HasIndex(s => s.BaseUrl);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.Type);
    }
}

