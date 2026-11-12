using Elegance.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace Elegance.Infrastructure.Notifications;

// The safe default for every environment until real Meta WhatsApp Business Cloud API credentials
// are configured (see docs for WhatsAppCloudApiService, added once those exist). This never
// claims a message actually reached a real phone - it only simulates the "the send request was
// accepted" step, and logs loudly every time it's used so a mock send is never mistaken for a
// real customer contact.
public class MockWhatsAppNotificationService(ILogger<MockWhatsAppNotificationService> logger) : IWhatsAppNotificationService
{
    public Task<WhatsAppSendResult> SendTemplateMessageAsync(
        string toE164, string templateName, IReadOnlyList<string> parameters, CancellationToken cancellationToken = default)
    {
        var messageId = $"mock-{Guid.NewGuid():N}";
        logger.LogWarning(
            "[Mock WhatsApp provider] No real message was sent. Would send template '{Template}' to {Number} with parameters [{Parameters}] (mock id {MessageId}). " +
            "Configure WhatsApp:Provider=CloudApi with real Meta credentials to send for real.",
            templateName, toE164, string.Join(" | ", parameters), messageId);

        return Task.FromResult(new WhatsAppSendResult(Accepted: true, MessageId: messageId, ErrorReason: null));
    }
}
