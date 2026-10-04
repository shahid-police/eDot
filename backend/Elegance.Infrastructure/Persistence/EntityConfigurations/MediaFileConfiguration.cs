using Elegance.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.StorageKey).HasMaxLength(500).IsRequired();
        builder.HasIndex(m => m.StorageKey).IsUnique();

        builder.Property(m => m.Url).HasMaxLength(1000).IsRequired();
        builder.Property(m => m.CdnUrl).HasMaxLength(1000);
        builder.Property(m => m.OriginalFilename).HasMaxLength(300);
        builder.Property(m => m.MimeType).HasMaxLength(100);
        builder.Property(m => m.AltText).HasMaxLength(300);
    }
}
