using Elegance.Domain.Notifications.Enums;

namespace Elegance.Domain.Notifications;

public enum VendorNotificationEvent
{
    // Admin-facing (sent to VendorNotificationOptions.AdminWhatsAppNumber, if configured).
    VendorSubmitted,
    VendorResubmitted,

    // Vendor-facing (sent to the vendor's own phone).
    SubmissionApproved,
    SubmissionRejected,
    ReturnedForCorrection,
}

// Mirrors Elegance.Domain.Notifications.WhatsAppNotification's queue/retry shape (reusing the
// same WhatsAppNotificationStatus enum and the same generic IWhatsAppNotificationService), but
// for vendor-module events rather than back-in-stock alerts. Queued synchronously at the point
// the triggering action happens (submission created/resubmitted/approved/rejected/returned);
// VendorNotificationWorker only needs the send-with-backoff half, not a separate detection step.
public class VendorNotification
{
    public long Id { get; set; }

    public VendorNotificationEvent Event { get; set; }

    // Set for vendor-facing events; null for admin-facing ones.
    public long? VendorId { get; set; }
    public long? SubmissionId { get; set; }

    // Denormalized destination number, same reasoning as WhatsAppNotification.WhatsAppNumber - a
    // later change to the vendor's phone must never rewrite what a historical send actually went to.
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
