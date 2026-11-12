using Elegance.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class NoShopVideoConfiguration : IEntityTypeConfiguration<NoShopVideo>
{
    public void Configure(EntityTypeBuilder<NoShopVideo> builder)
    {
        builder.ToTable("no_shop_videos");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Title).HasMaxLength(300).IsRequired();

        builder.Property(v => v.Slug).HasMaxLength(320).IsRequired();
        builder.HasIndex(v => v.Slug).IsUnique();

        builder.Property(v => v.YouTubeUrl).HasMaxLength(500).IsRequired();

        builder.Property(v => v.YouTubeVideoId).HasMaxLength(20).IsRequired();

        builder.Property(v => v.Description).HasMaxLength(1000);

        builder.Property(v => v.VideoType).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(v => v.SeoTitle).HasMaxLength(200);
        builder.Property(v => v.SeoDescription).HasMaxLength(500);

        builder.HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.CustomThumbnailMediaFile)
            .WithMany()
            .HasForeignKey(v => v.CustomThumbnailMediaFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.CategoryId);
        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.IsFeatured);
        builder.HasIndex(v => v.DisplayOrder);
        builder.HasIndex(v => v.PublishedAt);
    }
}
