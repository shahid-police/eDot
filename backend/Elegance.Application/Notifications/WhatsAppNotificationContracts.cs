namespace Elegance.Application.Notifications;

// Accepted only means the provider's API accepted the send request - never proof of delivery.
// Delivered/Read status only ever comes from a real webhook callback (see WhatsAppNotification's
// own status field, which is separate from this).
public record WhatsAppSendResult(bool Accepted, string? MessageId, string? ErrorReason);

// Kept deliberately provider-agnostic (template name + ordered parameters, matching how Meta's
// Cloud API itself expects a template send - not a pre-rendered message body) so a real
// WhatsAppCloudApiService can be swapped in later without changing any caller.
public interface IWhatsAppNotificationService
{
    Task<WhatsAppSendResult> SendTemplateMessageAsync(
        string toE164, string templateName, IReadOnlyList<string> parameters, CancellationToken cancellationToken = default);
}
