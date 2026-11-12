using System.Text.Json;

namespace Elegance.Application.Notifications;

public interface IWhatsAppWebhookService
{
    Task ProcessStatusUpdatesAsync(JsonElement payload, CancellationToken cancellationToken = default);
}
