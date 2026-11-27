using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Infrastructure.Admin;
using Elegance.Infrastructure.Inventory;
using Elegance.Tests.TestSupport;

namespace Elegance.Tests.Admin;

public class AdminCatalogServiceShoppingPreferenceTests
{
    private static AdminProductRequest MinimalProductRequest(long categoryId, string name, string? shoppingPreference = null) => new()
    {
        Name = name,
        Slug = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
        Price = 1000,
        Currency = "PKR",
        CategoryId = categoryId,
        ProductType = "Simple",
        StockQuantity = 1,
        ShoppingPreference = shoppingPreference,
    };

    [Fact]
    public async Task Creating_a_product_with_no_preference_leaves_it_unassigned()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));

        var created = await service.CreateProductAsync(new AdminActor(1, "admin@test.local"), MinimalProductRequest(category.Id, "Plain Ring"));

        Assert.Null(created.ShoppingPreference);
    }

    [Theory]
    [InlineData("Budget")]
    [InlineData("Premium")]
    [InlineData("Luxury")]
    [InlineData("budget")] // case-insensitive, matching every other enum field in this codebase
    public async Task Creating_a_product_saves_and_returns_the_chosen_preference(string value)
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));

        var created = await service.CreateProductAsync(new AdminActor(1, "admin@test.local"), MinimalProductRequest(category.Id, "Classified Ring", value));

        Assert.Equal(value, created.ShoppingPreference, ignoreCase: true);
    }

    [Fact]
    public async Task An_invalid_preference_value_is_rejected()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            service.CreateProductAsync(new AdminActor(1, "admin@test.local"), MinimalProductRequest(category.Id, "Bad Ring", "Expensive")));

        Assert.Equal(FailureKind.Validation, ex.Kind);
    }

    [Fact]
    public async Task Updating_a_product_preserves_its_classification_unless_changed()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");
        var created = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, "Stable Ring", "Luxury"));

        var request = new AdminProductRequest
        {
            Sku = created.Sku,
            Name = created.Name,
            Slug = created.Slug,
            Price = created.Price,
            Currency = created.Currency,
            CategoryId = created.CategoryId,
            ProductType = created.ProductType,
            StockQuantity = created.StockQuantity,
            ShoppingPreference = "Luxury",
        };
        var updated = await service.UpdateProductAsync(actor, created.Id, request);

        Assert.Equal("Luxury", updated.ShoppingPreference);
    }

    [Fact]
    public async Task Listing_filters_by_each_classification_and_by_unassigned()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");
        await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, "Budget Ring", "Budget"));
        await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, "Premium Ring", "Premium"));
        await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, "Luxury Ring", "Luxury"));
        await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, "Plain Ring"));

        var budget = await service.ListProductsAsync(new AdminProductQuery { ShoppingPreference = "Budget" });
        var premium = await service.ListProductsAsync(new AdminProductQuery { ShoppingPreference = "Premium" });
        var luxury = await service.ListProductsAsync(new AdminProductQuery { ShoppingPreference = "Luxury" });
        var unassigned = await service.ListProductsAsync(new AdminProductQuery { ShoppingPreference = "Unassigned" });
        var all = await service.ListProductsAsync(new AdminProductQuery());

        Assert.Equal("Budget Ring", Assert.Single(budget.Items).Name);
        Assert.Equal("Premium Ring", Assert.Single(premium.Items).Name);
        Assert.Equal("Luxury Ring", Assert.Single(luxury.Items).Name);
        Assert.Equal("Plain Ring", Assert.Single(unassigned.Items).Name);
        Assert.Equal(4, all.TotalCount);
    }
}
