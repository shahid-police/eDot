namespace Elegance.Domain.Catalog.Enums;

// Admin-assigned shopping preference classification (Budget/Premium/Luxury), used to power the
// customer-facing preference chips on the Homepage and Shop page. Null means Unassigned - legacy
// products are never auto-classified by price, and Unassigned products never appear under the
// Budget/Premium/Luxury chips (only under "Any Item").
public enum ShoppingPreference
{
    Budget,
    Premium,
    Luxury,
}
