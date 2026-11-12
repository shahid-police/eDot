namespace Elegance.Domain.Marketing.Enums;

public enum PromoterPaymentStatus
{
    // A real payment the admin recorded as sent.
    Recorded,

    // Admin corrected a mistaken entry - the payment row and its allocations are kept (never
    // deleted) for the audit trail, but no longer count towards any commission's paid amount.
    Voided,
}
