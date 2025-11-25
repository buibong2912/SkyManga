using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class CrawlJobConfiguration : IEntityTypeConfiguration<CrawlJob>
{
    public void Configure(EntityTypeBuilder<CrawlJob> builder)
    {
        builder.ToTable("CrawlJobs");

        // Primary Key
        builder.HasKey(cj => cj.Id);

        // Properties
        builder.Property(cj => cj.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(cj => cj.Description)
            .HasColumnType("text");

        builder.Property(cj => cj.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(cj => cj.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(cj => cj.ConfigurationJson)
            .HasColumnType("text");

        builder.Property(cj => cj.StartUrl)
            .HasMaxLength(2000);

        builder.Property(cj => cj.TotalItems)
            .HasDefaultValue(0);

        builder.Property(cj => cj.ProcessedItems)
            .HasDefaultValue(0);

        builder.Property(cj => cj.SuccessItems)
            .HasDefaultValue(0);

        builder.Property(cj => cj.FailedItems)
            .HasDefaultValue(0);

        builder.Property(cj => cj.ErrorMessage)
            .HasColumnType("text");

        builder.Property(cj => cj.StackTrace)
            .HasColumnType("text");

        builder.Property(cj => cj.CreatedAt)
            .IsRequired();

        builder.Property(cj => cj.UpdatedAt)
            .IsRequired();

        builder.Property(cj => cj.CreatedBy)
            .HasMaxLength(200);

        // Relationships
        builder.HasOne(cj => cj.Source)
            .WithMany(s => s.CrawlJobs)
            .HasForeignKey(cj => cj.SourceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(cj => cj.Manga)
            .WithMany()
            .HasForeignKey(cj => cj.MangaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(cj => cj.Logs)
            .WithOne(log => log.CrawlJob)
            .HasForeignKey(log => log.CrawlJobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(cj => cj.SourceId);
        builder.HasIndex(cj => cj.MangaId);
        builder.HasIndex(cj => cj.Type);
        builder.HasIndex(cj => cj.Status);
        builder.HasIndex(cj => cj.ScheduledAt);
        builder.HasIndex(cj => cj.StartedAt);
        builder.HasIndex(cj => cj.CreatedAt);
    }
}

