using Elegance.Domain.Catalog;
using Elegance.Domain.Notifications.Enums;

namespace Elegance.Domain.Notifications;

public class BackInStockSubscription
{
    public long Id { get; set; }

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Null means the subscription is for the simple product itself (no variants) - a customer
    // interested in one variant is never matched against a restock of a different variant.
    public long? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public required string CustomerName { get; set; }

    // As entered by the customer (kept for reference/display) - all matching and sending uses
    // NormalizedWhatsAppNumber instead.
    public required string WhatsAppNumber { get; set; }
    public required string NormalizedWhatsAppNumber { get; set; }

    public bool ConsentGiven { get; set; }
    public DateTime ConsentTimestamp { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    // Refreshed when an existing Active subscription is reused by a resubmission; CreatedAt never
    // changes once the row exists.
    public DateTime SubscribedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
