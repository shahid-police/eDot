using Elegance.Domain.Catalog;

namespace Elegance.Infrastructure.Catalog;

public static class PricingRules
{
    public static decimal UnitPrice(Product product, ProductVariant? variant) =>
        variant is null
            ? product.SalePrice ?? product.Price
            : variant.SalePrice ?? variant.Price ?? product.Price;
}
