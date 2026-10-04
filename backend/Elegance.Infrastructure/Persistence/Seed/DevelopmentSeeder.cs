using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only placeholder catalog data, never run outside ASPNETCORE_ENVIRONMENT=Development.
/// Not representative of real E. / elegence.pk inventory.
/// </summary>
public static class DevelopmentSeeder
{
    public static async Task SeedAsync(EleganceDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var rings = new Category { Name = "Rings", Slug = "rings", SortOrder = 1, CreatedAt = now, UpdatedAt = now };
        var necklaces = new Category { Name = "Necklaces", Slug = "necklaces", SortOrder = 2, CreatedAt = now, UpdatedAt = now };
        var earrings = new Category { Name = "Earrings", Slug = "earrings", SortOrder = 3, CreatedAt = now, UpdatedAt = now };

        dbContext.Categories.AddRange(rings, necklaces, earrings);

        MediaFile Media(string seed, string alt) => new()
        {
            StorageKey = $"seed/{seed}.jpg",
            Url = $"https://picsum.photos/seed/{seed}/800/1000",
            AltText = alt,
            CreatedAt = now,
        };

        var ringPrimary = Media("elegance-ring-1-primary", "Oval Halo Engagement Ring");
        var ringHover = Media("elegance-ring-1-hover", "Oval Halo Engagement Ring, alternate angle");

        var ring = new Product
        {
            Sku = "EL-RING-001",
            Name = "Oval Halo Engagement Ring",
            Slug = "oval-halo-engagement-ring",
            ShortDescription = "An oval-cut centrepiece ringed in pavé stones.",
            Description = "Placeholder seed description for local development only.",
            Price = 24900m,
            Currency = "PKR",
            Category = rings,
            ProductType = ProductType.Simple,
            Material = "sterling-silver",
            StockQuantity = 12,
            IsFeatured = true,
            IsNew = true,
            CreatedAt = now,
            UpdatedAt = now,
            Images =
            [
                new ProductImage { MediaFile = ringPrimary, ImageType = ProductImageType.Primary, SortOrder = 0, CreatedAt = now },
                new ProductImage { MediaFile = ringHover, ImageType = ProductImageType.Hover, SortOrder = 1, CreatedAt = now },
            ],
        };

        var necklacePrimary = Media("elegance-necklace-1-primary", "Kundan Bridal Necklace Set");
        var necklaceHover = Media("elegance-necklace-1-hover", "Kundan Bridal Necklace Set, alternate angle");
        var necklaceGoldImage = Media("elegance-necklace-1-gold", "Kundan Bridal Necklace Set, gold");
        var necklaceRoseImage = Media("elegance-necklace-1-rose", "Kundan Bridal Necklace Set, rose gold");

        var necklace = new Product
        {
            Sku = "EL-NCK-001",
            Name = "Kundan Bridal Necklace Set",
            Slug = "kundan-bridal-necklace-set",
            ShortDescription = "Hand-set kundan stones in a statement bridal silhouette.",
            Description = "Placeholder seed description for local development only.",
            Price = 48500m,
            Currency = "PKR",
            Category = necklaces,
            ProductType = ProductType.Variant,
            Material = "kundan",
            IsFeatured = true,
            IsBestSeller = true,
            CreatedAt = now,
            UpdatedAt = now,
            Images =
            [
                new ProductImage { MediaFile = necklacePrimary, ImageType = ProductImageType.Primary, SortOrder = 0, CreatedAt = now },
                new ProductImage { MediaFile = necklaceHover, ImageType = ProductImageType.Hover, SortOrder = 1, CreatedAt = now },
            ],
            Variants =
            [
                new ProductVariant
                {
                    Sku = "EL-NCK-001-GLD",
                    VariantName = "Gold",
                    Color = "Gold",
                    StockQuantity = 5,
                    ImageMediaFile = necklaceGoldImage,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new ProductVariant
                {
                    Sku = "EL-NCK-001-RG",
                    VariantName = "Rose Gold",
                    Color = "Rose Gold",
                    StockQuantity = 0,
                    ImageMediaFile = necklaceRoseImage,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
            ],
        };

        var earringPrimary = Media("elegance-earring-1-primary", "Jhumka Drop Earrings");
        var earringHover = Media("elegance-earring-1-hover", "Jhumka Drop Earrings, alternate angle");

        var earring = new Product
        {
            Sku = "EL-EAR-001",
            Name = "Jhumka Drop Earrings",
            Slug = "jhumka-drop-earrings",
            ShortDescription = "Traditional jhumka silhouette finished in antique gold.",
            Description = "Placeholder seed description for local development only.",
            Price = 15900m,
            Currency = "PKR",
            Category = earrings,
            ProductType = ProductType.Simple,
            Material = "brass",
            StockQuantity = 0,
            IsNew = true,
            CreatedAt = now,
            UpdatedAt = now,
            Images =
            [
                new ProductImage { MediaFile = earringPrimary, ImageType = ProductImageType.Primary, SortOrder = 0, CreatedAt = now },
                new ProductImage { MediaFile = earringHover, ImageType = ProductImageType.Hover, SortOrder = 1, CreatedAt = now },
            ],
        };

        dbContext.Products.AddRange(ring, necklace, earring);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
