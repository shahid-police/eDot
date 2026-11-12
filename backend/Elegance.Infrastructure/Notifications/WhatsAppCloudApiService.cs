using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Elegance.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Notifications;

// The official Meta WhatsApp Business Cloud API - the only sending path this project implements
// (no unofficial libraries, no browser automation, no personal WhatsApp accounts, per the spec).
// Requires an approved message template matching the parameter count/order used here; until one
// exists and WhatsApp:Provider is set to "CloudApi", MockWhatsAppNotificationService is what
// actually runs (see DependencyInjection's provider switch).
public class WhatsAppCloudApiService(HttpClient httpClient, IOptions<WhatsAppOptions> options, ILogger<WhatsAppCloudApiService> logger)
    : IWhatsAppNotificationService
{
    public async Task<WhatsAppSendResult> SendTemplateMessageAsync(
        string toE164, string templateName, IReadOnlyList<string> parameters, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;

        // Meta's "to" field is the number without the leading '+' - everything else in this
        // codebase (subscriptions, the DB column) keeps the full E.164 form with '+', so the
        // conversion happens right here at the API boundary only.
        var to = toE164.TrimStart('+');

        var requestBody = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = opts.TemplateLanguage },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameters.Select(p => new { type = "text", text = p }).ToArray(),
                    },
                },
            },
        };

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/{opts.ApiVersion}/{opts.PhoneNumberId}/messages")
            {
                Content = JsonContent.Create(requestBody),
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.AccessToken);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = document.RootElement
                    .GetProperty("messages")[0]
                    .GetProperty("id")
                    .GetString();
                return new WhatsAppSendResult(Accepted: true, MessageId: messageId, ErrorReason: null);
            }

            var errorMessage = document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : $"WhatsApp API returned {(int)response.StatusCode}.";

            logger.LogWarning("WhatsApp Cloud API send to {Number} failed ({Status}): {Error}", toE164, response.StatusCode, errorMessage);
            return new WhatsAppSendResult(Accepted: false, MessageId: null, ErrorReason: errorMessage);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "WhatsApp Cloud API send to {Number} failed", toE164);
            return new WhatsAppSendResult(Accepted: false, MessageId: null, ErrorReason: ex.Message);
        }
    }
}
