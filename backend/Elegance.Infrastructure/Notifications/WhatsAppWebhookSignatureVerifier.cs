using System.Security.Cryptography;
using System.Text;

namespace Elegance.Infrastructure.Notifications;

// Verifies Meta's X-Hub-Signature-256 header on every webhook POST, per Meta's documented
// webhook security requirements - without this, anyone who discovers the webhook URL could post
// forged delivery-status updates (e.g. falsely marking a notification "Delivered").
public static class WhatsAppWebhookSignatureVerifier
{
    public static bool IsValid(ReadOnlySpan<byte> rawBody, string? signatureHeader, string appSecret)
    {
        if (string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(signatureHeader))
        {
            return false;
        }

        const string prefix = "sha256=";
        if (!signatureHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var providedHex = signatureHeader[prefix.Length..];
        var computed = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), rawBody);
        var computedHex = Convert.ToHexStringLower(computed);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedHex),
            Encoding.ASCII.GetBytes(providedHex));
    }
}
