namespace Elegance.Domain.Notifications.Enums;

public enum WhatsAppNotificationStatus
{
    // Queued by the detection step, not yet picked up by the send loop.
    Pending,

    // Claimed by the send loop, API call in flight or about to be made.
    Queued,

    // The WhatsApp API accepted the request - not proof of delivery, only that it was sent.
    Sent,

    // Confirmed via webhook.
    Delivered,
    Read,

    // Exhausted retries or a non-retryable API error. See FailureReason.
    Failed
}
