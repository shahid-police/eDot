using Elegance.Application.Admin;
using Elegance.Infrastructure.Admin;
using Elegance.Infrastructure.Inventory;
using Elegance.Tests.TestSupport;

namespace Elegance.Tests.Admin;

public class AdminCatalogServiceSortTests
{
    [Fact]
    public async Task Sorting_by_name_ascending_and_descending_orders_correctly()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-001", slug: "zed-product");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-002", slug: "alpha-product");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));

        var asc = await service.ListProductsAsync(new AdminProductQuery { SortBy = "name", SortDescending = false });
        var desc = await service.ListProductsAsync(new AdminProductQuery { SortBy = "name", SortDescending = true });

        // TestProduct.CreateAsync sets Name = sku, so "JEW-NSET-001" < "JEW-NSET-002" alphabetically.
        Assert.Equal("JEW-NSET-001", asc.Items[0].Sku);
        Assert.Equal("JEW-NSET-002", asc.Items[1].Sku);
        Assert.Equal("JEW-NSET-002", desc.Items[0].Sku);
        Assert.Equal("JEW-NSET-001", desc.Items[1].Sku);
    }

    // No automated test for SortBy "price" here: the Sqlite test provider cannot translate
    // ORDER BY over a decimal column at all ("SQLite does not support expressions of type
    // 'decimal' in ORDER BY clauses") regardless of the expression used, which is a test-provider
    // limitation, not a production one - MySQL (Pomelo), what this app actually runs on, orders
    // decimal columns natively. Verified instead via a live request against the real database (see
    // deploy notes) using the same SalePrice ?? Price expression the Products table displays.

    [Fact]
    public async Task No_sort_specified_keeps_the_original_newest_first_default()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-001");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-002");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context), new RestockDetectionService(db.Context));

        var result = await service.ListProductsAsync(new AdminProductQuery());

        Assert.Equal("JEW-NSET-002", result.Items[0].Sku);
        Assert.Equal("JEW-NSET-001", result.Items[1].Sku);
    }
}
