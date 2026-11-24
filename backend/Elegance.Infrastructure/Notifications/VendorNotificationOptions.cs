namespace Elegance.Infrastructure.Notifications;

public class VendorNotificationOptions
{
    public const string SectionName = "VendorNotifications";

    // Admin-facing events (a vendor submitted/resubmitted a product) go to this single number if
    // set. Left blank by default - there's no "admin phone number" concept elsewhere in the app,
    // so until an admin configures one, those two events are simply not queued (no-op, not an
    // error) rather than forcing a number to exist.
    public string? AdminWhatsAppNumber { get; set; }

    // Each needs a real, Meta-approved template (see WhatsAppOptions.Provider) before CloudApi can
    // actually send it - these are the template names to request approval for.
    public string SubmittedTemplateName { get; set; } = "vendor_submission_received";
    public string ResubmittedTemplateName { get; set; } = "vendor_submission_resubmitted";
    public string ApprovedTemplateName { get; set; } = "vendor_submission_approved";
    public string RejectedTemplateName { get; set; } = "vendor_submission_rejected";
    public string ReturnedForCorrectionTemplateName { get; set; } = "vendor_submission_returned";

    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 60;
    public int PollIntervalSeconds { get; set; } = 20;
}
