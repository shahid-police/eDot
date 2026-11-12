namespace Elegance.Domain.Marketing;

// One row per (payment, commission) pair this payment covers, in whole or in part.
public class PromoterPaymentAllocation
{
    public long Id { get; set; }

    public long PromoterPaymentId { get; set; }
    public PromoterPayment PromoterPayment { get; set; } = null!;

    public long PromoterCommissionId { get; set; }
    public PromoterCommission PromoterCommission { get; set; } = null!;

    public decimal AllocatedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
