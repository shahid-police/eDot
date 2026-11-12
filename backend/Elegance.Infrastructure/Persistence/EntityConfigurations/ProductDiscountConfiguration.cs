using Elegance.Domain.Discounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class ProductDiscountConfiguration : IEntityTypeConfiguration<ProductDiscount>
{
    public void Configure(EntityTypeBuilder<ProductDiscount> builder)
    {
        builder.ToTable("product_discounts");

        builder.Property(d => d.OriginalPriceSnapshot).HasPrecision(10, 2);
        builder.Property(d => d.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(d => d.DiscountAmount).HasPrecision(10, 2);
        builder.Property(d => d.FinalPriceSnapshot).HasPrecision(10, 2);

        builder.HasOne(d => d.Product)
            .WithMany(p => p.Discounts)
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ProductVariant)
            .WithMany(v => v.Discounts)
            .HasForeignKey(d => d.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        // The hot path (PricingRules, product listing/detail) always filters on exactly this
        // triple, plus a date-range check - see the index below for that half.
        builder.HasIndex(d => new { d.ProductId, d.ProductVariantId, d.Status });
        builder.HasIndex(d => new { d.StartDate, d.EndDate });
    }
}
