using Elegance.Domain.Catalog;
using Elegance.Domain.Discounts;
using Elegance.Domain.Discounts.Enums;

namespace Elegance.Infrastructure.Catalog;

public static class PricingRules
{
    // A currently-active ProductDiscount overrides SalePrice; SalePrice remains the fallback for
    // a product that's never had one scheduled. Callers must have loaded product.Discounts /
    // variant.Discounts (ideally pre-filtered to Active + in date range, as the Includes in
    // CartService/CheckoutService do) - an unloaded collection is just empty, so this quietly
    // falls back to SalePrice rather than throwing, which is the safe failure mode either way.
    public static decimal UnitPrice(Product product, ProductVariant? variant)
    {
        var discounts = variant?.Discounts ?? product.Discounts;
        var activeDiscount = CurrentlyActive(discounts);
        return activeDiscount?.FinalPriceSnapshot ?? NormalPrice(product, variant);
    }

    // What the customer would pay without a currently-active ProductDiscount - used to show
    // "was X, now Y" on an order line even though UnitPrice itself only ever holds the one price
    // that was actually charged.
    public static decimal NormalPrice(Product product, ProductVariant? variant) =>
        variant is null
            ? product.SalePrice ?? product.Price
            : variant.SalePrice ?? variant.Price ?? product.Price;

    private static ProductDiscount? CurrentlyActive(IEnumerable<ProductDiscount> discounts)
    {
        var now = DateTime.UtcNow;
        return discounts
            .Where(d => d.Status == ProductDiscountStatus.Active && d.StartDate <= now && d.EndDate >= now)
            .OrderByDescending(d => d.Id)
            .FirstOrDefault();
    }
}
