using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class CrawlJobLogConfiguration : IEntityTypeConfiguration<CrawlJobLog>
{
    public void Configure(EntityTypeBuilder<CrawlJobLog> builder)
    {
        builder.ToTable("CrawlJobLogs");

        // Primary Key
        builder.HasKey(log => log.Id);

        // Properties
        builder.Property(log => log.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.Level)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(log => log.Exception)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.StackTrace)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.Url)
            .HasMaxLength(2000);

        builder.Property(log => log.AdditionalData)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(log => log.CrawlJob)
            .WithMany(cj => cj.Logs)
            .HasForeignKey(log => log.CrawlJobId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Indexes
        builder.HasIndex(log => log.CrawlJobId);
        builder.HasIndex(log => log.Level);
        builder.HasIndex(log => log.CreatedAt);
        builder.HasIndex(log => new { log.CrawlJobId, log.CreatedAt });
    }
}

