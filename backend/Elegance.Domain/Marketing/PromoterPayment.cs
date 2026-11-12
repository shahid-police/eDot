using Elegance.Domain.Marketing.Enums;

namespace Elegance.Domain.Marketing;

// An actual settlement sent to a promoter - distinct from PromoterCommission (an amount owed).
// One payment can cover several commissions (Allocations), and one commission can be settled
// across several payments over time - see PromoterPaymentAllocation.
public class PromoterPayment
{
    public long Id { get; set; }

    // "PMT-{year}-{Id:D6}" - same placeholder-then-rename pattern Order.OrderNumber already uses,
    // since the real Id isn't known until after the first insert.
    public required string PaymentReference { get; set; }

    public long PromoterId { get; set; }
    public Promoter Promoter { get; set; } = null!;

    // Always equal to the sum of this payment's own Allocations - computed once at creation, never
    // recalculated afterward (a payment's allocations never change after the fact; voiding the
    // whole payment is the only way to undo one).
    public decimal TotalPaymentAmount { get; set; }

    public DateTime PaymentDate { get; set; }

    // Free text (BankTransfer, EasyPaisa, JazzCash, Cash, ...) - the actual set of payment rails
    // promoters get paid through is business/region-specific, not something to hardcode as an enum.
    public required string PaymentMethod { get; set; }

    public string? TransactionReference { get; set; }

    public PromoterPaymentStatus Status { get; set; } = PromoterPaymentStatus.Recorded;
    public string? Remarks { get; set; }

    public long CreatedByAdminUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PromoterPaymentAllocation> Allocations { get; set; } = [];
}
