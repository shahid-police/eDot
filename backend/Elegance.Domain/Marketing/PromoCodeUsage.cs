using Elegance.Domain.Customers;
using Elegance.Domain.Orders;

namespace Elegance.Domain.Marketing;

// One row per order that actually used a promo code - the attribution record Phase 6 reporting
// and MaximumUsesPerCustomer enforcement both read from. Written atomically with the order itself
// (same transaction, same CheckoutService.PlaceOrderAsync call).
public class PromoCodeUsage
{
    public long Id { get; set; }

    public long PromoCodeId { get; set; }
    public PromoCode PromoCode { get; set; } = null!;

    // Denormalized from PromoCode.PromoterId - same convenience as Order.PromoterId, avoids a
    // join through PromoCode for every reporting query.
    public long PromoterId { get; set; }
    public Promoter Promoter { get; set; } = null!;

    // Null for a guest checkout - MaximumUsesPerCustomer can only be enforced for a signed-in
    // customer, since a guest has no durable identity to count repeat use against.
    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
}
