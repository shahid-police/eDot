using Elegance.Application.Vendors;
using Elegance.Domain.Catalog;
using Elegance.Domain.Orders.Enums;
using Elegance.Tests.TestSupport;

namespace Elegance.Tests.Vendors;

public class VendorOrderServiceTests
{
    private static async Task<(Elegance.Domain.Catalog.Product Owned, Elegance.Domain.Catalog.Product Other)> SeedProductsAsync(
        VendorAuthTestContext ctx, long vendorId, long categoryId)
    {
        var now = DateTime.UtcNow;
        var owned = new Elegance.Domain.Catalog.Product
        {
            Sku = "JEW-TEST-OWNED", Name = "Owned Ring", Slug = "owned-ring",
            CategoryId = categoryId, VendorId = vendorId, CreatedAt = now, UpdatedAt = now,
        };
        var other = new Elegance.Domain.Catalog.Product
        {
            Sku = "JEW-TEST-OTHER", Name = "Other Ring", Slug = "other-ring",
            CategoryId = categoryId, CreatedAt = now, UpdatedAt = now,
        };
        ctx.Db.Context.Products.AddRange(owned, other);
        await ctx.Db.Context.SaveChangesAsync();
        return (owned, other);
    }

    [Fact]
    public async Task Summary_counts_only_orders_containing_this_vendors_products()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var (owned, other) = await SeedProductsAsync(ctx, vendor.Id, category.Id);

        await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 2, unitPrice: 1000, status: OrderStatus.Delivered);
        await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 1, unitPrice: 1000, status: OrderStatus.Pending);
        await TestOrder.CreateAsync(ctx.Db.Context, other.Id, other.Sku, other.Name, quantity: 5, unitPrice: 500, status: OrderStatus.Delivered);

        var summary = await ctx.OrderService.GetSummaryAsync(vendor.Id);

        Assert.Equal(2, summary.TotalOrders);
        Assert.Equal(3, summary.TotalUnits);
        Assert.Equal(2, summary.DeliveredUnits);
        Assert.Equal(1, summary.DeliveredOrders);
        Assert.Equal(1, summary.PendingOrders);
    }

    [Fact]
    public async Task ListOrderItems_never_returns_another_vendors_items()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var (owned, other) = await SeedProductsAsync(ctx, vendor.Id, category.Id);

        await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 1, unitPrice: 1000);
        await TestOrder.CreateAsync(ctx.Db.Context, other.Id, other.Sku, other.Name, quantity: 1, unitPrice: 500);

        var result = await ctx.OrderService.ListOrderItemsAsync(vendor.Id, new VendorOrderQuery());

        Assert.Single(result.Items);
        Assert.Equal(owned.Sku, result.Items[0].Sku);
    }

    [Fact]
    public async Task OrderDetail_redacts_customer_name_phone_and_street_address()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var (owned, _) = await SeedProductsAsync(ctx, vendor.Id, category.Id);
        var order = await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 1, unitPrice: 1000, city: "Karachi", province: "Sindh");

        var detail = await ctx.OrderService.GetOrderDetailAsync(vendor.Id, order.Id);

        Assert.Equal("Karachi", detail.City);
        Assert.Equal("Sindh", detail.Province);
        Assert.Single(detail.Items);
        // No property on the DTO at all can carry a full name, phone, or street address - confirmed
        // structurally, not just by omission in this one assertion.
        var dtoProperties = typeof(VendorOrderDetailDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain("CustomerName", dtoProperties);
        Assert.DoesNotContain("Phone", dtoProperties);
        Assert.DoesNotContain("AddressLine1", dtoProperties);
    }

    [Fact]
    public async Task OrderDetail_for_an_order_with_none_of_this_vendors_items_is_not_found()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var (_, other) = await SeedProductsAsync(ctx, vendor.Id, category.Id);
        var order = await TestOrder.CreateAsync(ctx.Db.Context, other.Id, other.Sku, other.Name, quantity: 1, unitPrice: 500);

        await Assert.ThrowsAsync<Elegance.Application.Common.ApplicationFailure>(() => ctx.OrderService.GetOrderDetailAsync(vendor.Id, order.Id));
    }

    [Fact]
    public async Task SalesAnalytics_excludes_cancelled_and_returned_units_from_gross_sales_and_best_sellers()
    {
        using var ctx = new VendorAuthTestContext();
        var category = await TestCategory.CreateAsync(ctx.Db.Context);
        var vendor = await TestVendor.CreateAsync(ctx.Db.Context);
        var (owned, _) = await SeedProductsAsync(ctx, vendor.Id, category.Id);

        await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 3, unitPrice: 100, status: OrderStatus.Delivered);
        await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 4, unitPrice: 100, status: OrderStatus.Cancelled);
        await TestOrder.CreateAsync(ctx.Db.Context, owned.Id, owned.Sku, owned.Name, quantity: 2, unitPrice: 100, status: OrderStatus.Returned);

        var analytics = await ctx.OrderService.GetSalesAnalyticsAsync(vendor.Id);

        Assert.Equal(3, analytics.TotalUnitsSold);
        Assert.Equal(300, analytics.TotalGrossSalesValue);
        Assert.Equal(4, analytics.CancelledUnits);
        Assert.Equal(2, analytics.ReturnedUnits);
        Assert.Single(analytics.BestSellingProducts);
        Assert.Equal(3, analytics.BestSellingProducts[0].UnitsSold);
    }
}
