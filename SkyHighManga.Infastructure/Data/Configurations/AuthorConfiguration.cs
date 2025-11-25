using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyHighManga.Domain.Entities;

namespace SkyHighManga.Infastructure.Data.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.AlternativeName)
            .HasMaxLength(200);

        builder.Property(a => a.Bio)
            .HasColumnType("text");

        builder.Property(a => a.ProfileImageUrl)
            .HasMaxLength(2000);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasMany(a => a.Mangas)
            .WithOne(m => m.Author)
            .HasForeignKey(m => m.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.IsActive);
    }
}

