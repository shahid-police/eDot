namespace Elegance.Domain.Discounts.Enums;

// Only the admin-controlled state is persisted. Whether a discount is currently "Scheduled",
// "Active" (in effect right now) or "Expired" is always derived from Status == Active plus
// StartDate/EndDate compared against the current time - see AdminProductDiscountService and
// PricingRules, never stored redundantly as a value that would need a background job to update.
public enum ProductDiscountStatus
{
    Active,
    Cancelled,
}
