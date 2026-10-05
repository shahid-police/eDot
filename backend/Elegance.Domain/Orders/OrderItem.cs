using Elegance.Domain.Catalog;

namespace Elegance.Domain.Orders;

public class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public long? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public string? VariantName { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
