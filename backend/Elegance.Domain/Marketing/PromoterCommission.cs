using Elegance.Domain.Marketing.Enums;
using Elegance.Domain.Orders;

namespace Elegance.Domain.Marketing;

// One row per order that earned a promoter a commission - created Pending at order placement
// (CheckoutService.PlaceOrderAsync) directly from that order's own CommissionPercentageSnapshot/
// CommissionEligibleAmount, which are themselves frozen at checkout and never recomputed here.
public class PromoterCommission
{
    public long Id { get; set; }

    public long PromoterId { get; set; }
    public Promoter Promoter { get; set; } = null!;

    public long PromoCodeId { get; set; }
    public PromoCode PromoCode { get; set; } = null!;

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal CommissionPercentageSnapshot { get; set; }
    public decimal EligibleSaleAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Currency { get; set; } = "PKR";

    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;

    // Required whenever Status moves to Reversed or Cancelled - why, for the audit trail.
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PayableAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ReversedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
