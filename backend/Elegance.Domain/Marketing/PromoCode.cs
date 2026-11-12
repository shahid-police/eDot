using Elegance.Domain.Marketing.Enums;

namespace Elegance.Domain.Marketing;

public class PromoCode
{
    public long Id { get; set; }

    // Exactly six numeric digits, stored as a string to preserve leading zeros (e.g. "048213").
    // Generated via PromoCodeGenerator - never sequential, unique by DB constraint.
    public required string Code { get; set; }

    public long PromoterId { get; set; }
    public Promoter Promoter { get; set; } = null!;

    // A code can grant a customer discount, promoter commission, or both - never neither
    // (validated in AdminPromoCodeService).
    public bool GrantsCustomerDiscount { get; set; }
    public decimal CustomerDiscountPercentage { get; set; }

    public bool GrantsCommission { get; set; } = true;

    // Null means "use the promoter's current CommissionPercentage" - set only for a campaign that
    // needs a different rate than that promoter's usual one.
    public decimal? CommissionPercentageOverride { get; set; }

    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }

    // Null means unlimited.
    public int? MaximumTotalUses { get; set; }
    public int? MaximumUsesPerCustomer { get; set; }

    // Incremented atomically at checkout (Phase 3) - not touched by anything in this phase yet.
    public int UsedCount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public PromoCodeStatus Status { get; set; } = PromoCodeStatus.Active;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Empty on both means "all eligible products" (catalogue-wide) - restricting to specific
    // products and/or categories is additive (either list matching is enough), not an AND.
    public ICollection<PromoCodeProduct> Products { get; set; } = [];
    public ICollection<PromoCodeCategory> Categories { get; set; } = [];
}
