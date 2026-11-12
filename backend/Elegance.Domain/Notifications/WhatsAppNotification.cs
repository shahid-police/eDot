using Elegance.Domain.Catalog;
using Elegance.Domain.Inventory;
using Elegance.Domain.Notifications.Enums;

namespace Elegance.Domain.Notifications;

// One row per (subscription, restock event) pair - the unique index on that pair is what makes
// "don't send the same subscriber two messages for the same restock" an actual database
// guarantee, not just application logic that could be bypassed by a re-run of the detection step.
public class WhatsAppNotification
{
    public long Id { get; set; }

    public long SubscriptionId { get; set; }
    public BackInStockSubscription Subscription { get; set; } = null!;

    public long RestockEventId { get; set; }
    public RestockEvent RestockEvent { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Denormalized onto the notification row (not just reachable via Subscription) so a change to
    // the subscription's number later never rewrites what a historical send actually went to.
    public required string WhatsAppNumber { get; set; }

    public string? WhatsAppMessageId { get; set; }
    public string? MessageTemplateName { get; set; }

    public WhatsAppNotificationStatus Status { get; set; } = WhatsAppNotificationStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? FailureReason { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
