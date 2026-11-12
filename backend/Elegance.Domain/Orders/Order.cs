using Elegance.Domain.Customers;
using Elegance.Domain.Marketing;
using Elegance.Domain.Orders.Enums;

namespace Elegance.Domain.Orders;

public class Order
{
    public long Id { get; set; }
    public required string OrderNumber { get; set; }

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? GuestTokenHash { get; set; }

    public required string Email { get; set; }
    public required string Phone { get; set; }

    public decimal Subtotal { get; set; }

    // Informational rollup only - already folded into Subtotal (OrderItem.UnitPrice is the
    // post-product-discount price). Lets order history show "Product discounts: -X" as its own
    // line without re-deriving it from every item's OriginalUnitPrice.
    public decimal ProductDiscountTotal { get; set; }

    public decimal ShippingTotal { get; set; }
    public decimal TaxTotal { get; set; }

    // The one promo code applied to this order, if any - frozen here even if the code is later
    // edited, deactivated, or its promoter reassigned (it can't be, but if that ever changed,
    // this order's attribution must not).
    public long? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }

    public long? PromoterId { get; set; }
    public Promoter? Promoter { get; set; }

    public decimal? PromoCodeDiscountPercentageSnapshot { get; set; }
    public decimal PromoCodeDiscountAmount { get; set; }

    // The commission rate and base this order owes its promoter, if GrantsCommission was true on
    // the applied code - Phase 4 reads these directly rather than re-deriving them from the code's
    // (possibly since-changed) configuration.
    public decimal? CommissionPercentageSnapshot { get; set; }
    public decimal? CommissionEligibleAmount { get; set; }

    // Subtotal + ShippingTotal + TaxTotal - PromoCodeDiscountAmount.
    public decimal GrandTotal { get; set; }
    public required string Currency { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderAddress> Addresses { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
