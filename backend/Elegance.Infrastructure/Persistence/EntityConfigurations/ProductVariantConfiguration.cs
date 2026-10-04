using Elegance.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(v => v.Sku).IsUnique();

        builder.Property(v => v.VariantName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Price).HasPrecision(10, 2);
        builder.Property(v => v.SalePrice).HasPrecision(10, 2);
        builder.Property(v => v.WeightGrams).HasPrecision(10, 2);
        builder.Property(v => v.Size).HasMaxLength(50);
        builder.Property(v => v.Color).HasMaxLength(50);

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.ImageMediaFile)
            .WithMany()
            .HasForeignKey(v => v.ImageMediaFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.ProductId);
    }
}
