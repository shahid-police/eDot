using Elegance.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageType).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.MediaFile)
            .WithMany()
            .HasForeignKey(i => i.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.ProductId);
        builder.HasIndex(i => i.VariantId);
    }
}
