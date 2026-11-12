using Elegance.Domain.Marketing.Enums;

namespace Elegance.Domain.Marketing;

public class Promoter
{
    public long Id { get; set; }

    public required string Name { get; set; }

    // E.164, normalized the same way as BackInStockSubscription's - see PhoneNumberNormalizer.
    public required string WhatsAppNumber { get; set; }

    public string? Email { get; set; }
    public string? Address { get; set; }

    public PromoterType PromoterType { get; set; } = PromoterType.Individual;

    // The default commission percentage for this promoter's codes - a PromoCode may override it
    // for a specific campaign via CommissionPercentageOverride, but this is what's inherited when
    // it doesn't.
    public decimal CommissionPercentage { get; set; }

    public PromoterStatus Status { get; set; } = PromoterStatus.Active;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PromoCode> PromoCodes { get; set; } = [];
}
