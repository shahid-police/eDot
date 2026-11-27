using Elegance.Application.Products;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Infrastructure.Catalog;
using Elegance.Tests.TestSupport;

namespace Elegance.Tests.Catalog;

public class ProductServiceShoppingPreferenceTests
{
    private static async Task SeedAsync(Elegance.Infrastructure.Persistence.EleganceDbContext db, long categoryId)
    {
        var now = DateTime.UtcNow;
        db.Products.AddRange(
            new Product { Sku = "B-1", Name = "Budget Ring", Slug = "budget-ring", CategoryId = categoryId, IsActive = true, ShoppingPreference = ShoppingPreference.Budget, CreatedAt = now, UpdatedAt = now },
            new Product { Sku = "P-1", Name = "Premium Ring", Slug = "premium-ring", CategoryId = categoryId, IsActive = true, ShoppingPreference = ShoppingPreference.Premium, CreatedAt = now, UpdatedAt = now },
            new Product { Sku = "L-1", Name = "Luxury Ring", Slug = "luxury-ring", CategoryId = categoryId, IsActive = true, ShoppingPreference = ShoppingPreference.Luxury, CreatedAt = now, UpdatedAt = now },
            new Product { Sku = "U-1", Name = "Unassigned Ring", Slug = "unassigned-ring", CategoryId = categoryId, IsActive = true, ShoppingPreference = null, CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Any_item_with_no_preference_filter_returns_everything_including_unassigned()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context);
        await SeedAsync(db.Context, category.Id);
        var service = new ProductService(db.Context);

        var result = await service.GetProductsAsync(new ProductListQuery());

        Assert.Equal(4, result.TotalCount);
    }

    [Theory]
    [InlineData("budget", "Budget Ring")]
    [InlineData("premium", "Premium Ring")]
    [InlineData("luxury", "Luxury Ring")]
    public async Task A_specific_preference_returns_only_that_classification_never_unassigned(string preference, string expectedName)
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context);
        await SeedAsync(db.Context, category.Id);
        var service = new ProductService(db.Context);

        var result = await service.GetProductsAsync(new ProductListQuery { ShoppingPreference = preference });

        Assert.Equal(expectedName, Assert.Single(result.Items).Name);
    }
}
