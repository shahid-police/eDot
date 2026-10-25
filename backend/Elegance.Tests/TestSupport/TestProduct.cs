using Elegance.Domain.Catalog;
using Elegance.Infrastructure.Persistence;

namespace Elegance.Tests.TestSupport;

// For seeding "existing products" directly, to test SKU-generation logic against real rows
// without going through AdminCatalogService.CreateProductAsync itself.
public static class TestProduct
{
    public static async Task<Product> CreateAsync(EleganceDbContext db, long categoryId, string sku, string? slug = null)
    {
        var product = new Product
        {
            Sku = sku,
            Name = sku,
            Slug = slug ?? sku.ToLowerInvariant(),
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
