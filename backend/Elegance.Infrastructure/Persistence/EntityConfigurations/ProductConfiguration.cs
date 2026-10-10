using Elegance.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elegance.Infrastructure.Persistence.EntityConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(300).IsRequired();

        builder.Property(p => p.Slug).HasMaxLength(320).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.ShortDescription).HasMaxLength(500);
        builder.Property(p => p.Description).HasColumnType("text");

        builder.Property(p => p.Price).HasPrecision(10, 2);
        builder.Property(p => p.SalePrice).HasPrecision(10, 2);
        builder.Property(p => p.CostPrice).HasPrecision(10, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.Property(p => p.ProductType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.Material).HasMaxLength(100);
        builder.Property(p => p.Color).HasMaxLength(50);
        builder.Property(p => p.Occasion).HasMaxLength(100);
        builder.Property(p => p.WeightGrams).HasPrecision(10, 2);

        builder.Property(p => p.SeoTitle).HasMaxLength(200);
        builder.Property(p => p.SeoDescription).HasMaxLength(500);
        builder.Property(p => p.SeoKeywords).HasMaxLength(500);

        builder.Property(p => p.VideoUrl).HasMaxLength(500);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => p.IsNew);
        builder.HasIndex(p => p.IsBestSeller);
        builder.HasIndex(p => p.CreatedAt);
    }
}
