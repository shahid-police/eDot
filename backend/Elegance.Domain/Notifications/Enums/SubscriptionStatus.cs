namespace Elegance.Domain.Notifications.Enums;

public enum SubscriptionStatus
{
    // Waiting for the next restock of this exact product/variant.
    Active,

    // Successfully notified for a restock - terminal. The customer must subscribe again to be
    // notified of a future restock after the item sells out again.
    Notified,

    // Customer opted out - terminal.
    Unsubscribed
}
