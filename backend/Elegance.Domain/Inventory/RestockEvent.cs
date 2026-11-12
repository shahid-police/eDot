namespace Elegance.Domain.Inventory;

// Recorded only on a genuine zero-to-positive stock transition (never on an ordinary quantity
// change, e.g. 5 -> 10) - see IRestockDetectionService. This is what the back-in-stock
// notification worker fans out from: one row per restock, one WhatsAppNotification per eligible
// subscription for that row.
public class RestockEvent
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long? VariantId { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public DateTime DetectedAt { get; set; }

    // Set once the notification worker has fanned this event out into WhatsAppNotification rows
    // (or determined there was nothing eligible to notify) - null means still pending pickup.
    public DateTime? ProcessedAt { get; set; }
}
