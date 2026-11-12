using System.Text.Json;
using Elegance.Application.Notifications;
using Elegance.Infrastructure.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elegance.Api.Controllers.V1;

// Public by design - Meta's servers call this directly with no admin session. Authenticated
// instead via the webhook verify token (GET handshake) and the X-Hub-Signature-256 HMAC (every
// POST), per Meta's own webhook security model - not this app's JWT auth, which Meta can't use.
[ApiController]
[Route("api/v1/whatsapp/webhook")]
public class WhatsAppWebhookController(
    IWhatsAppWebhookService webhookService,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var configuredToken = options.Value.WebhookVerifyToken;
        if (mode == "subscribe" && !string.IsNullOrEmpty(configuredToken) && verifyToken == configuredToken)
        {
            return Content(challenge ?? string.Empty, "text/plain");
        }

        logger.LogWarning("Rejected WhatsApp webhook verification attempt (mode={Mode}).", mode);
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var rawBody = buffer.ToArray();

        var signature = Request.Headers["X-Hub-Signature-256"].ToString();
        if (!WhatsAppWebhookSignatureVerifier.IsValid(rawBody, signature, options.Value.AppSecret))
        {
            logger.LogWarning("Rejected a WhatsApp webhook delivery with an invalid or missing signature.");
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        using var document = JsonDocument.Parse(rawBody);
        await webhookService.ProcessStatusUpdatesAsync(document.RootElement, cancellationToken);

        // Meta expects a fast 200 regardless of content and retries delivery otherwise.
        return Ok();
    }
}
