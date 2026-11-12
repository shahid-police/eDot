namespace Elegance.Domain.Marketing.Enums;

// Only the admin-controlled state is persisted (Active/Inactive). Whether a code is currently
// "Scheduled" (before StartDate) or "Expired" (after ExpiryDate) is always derived against the
// current time - see AdminPromoCodeService, the same pattern ProductDiscount already uses.
public enum PromoCodeStatus
{
    Active,
    Inactive,
}
