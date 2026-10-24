using OtpNet;

namespace Elegance.Infrastructure.Auth;

// RFC 6238/4226 TOTP, Google Authenticator's default profile: 6 digits, 30s step, SHA1,
// +/-1 step verification window. Wraps Otp.NET so no primitive is hand-rolled here.
// TimeProvider is injected (not DateTime.UtcNow) so tests can use a fake clock to generate a
// code at one instant and verify it - or verify expiry - deterministically, with no sleeping.
public class TotpService(TimeProvider timeProvider)
{
    private const int SecretBytesLength = 20; // 160 bits
    private static readonly VerificationWindow Window = new(previous: 1, future: 1);

    public byte[] GenerateSecret() => KeyGeneration.GenerateRandomKey(SecretBytesLength);

    public string ToBase32(byte[] secret) => Base32Encoding.ToString(secret);

    public string BuildOtpAuthUri(byte[] secret, string issuer, string accountLabel)
    {
        var secret32 = Base32Encoding.ToString(secret);
        var label = Uri.EscapeDataString($"{issuer}:{accountLabel}");
        var query = $"secret={secret32}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30&algorithm=SHA1";
        return $"otpauth://totp/{label}?{query}";
    }

    // Returns the matched time step on success, so the caller can persist it and reject replay
    // of the same (or an older) step. Returns null for an invalid or already-expired code.
    public long? VerifyCode(byte[] secret, string code, long lastAcceptedTimeStep)
    {
        var normalized = code.Trim();
        if (normalized.Length != 6 || !normalized.All(char.IsDigit))
        {
            return null;
        }

        var totp = new Totp(secret, step: 30, totpSize: 6);
        if (!totp.VerifyTotp(timeProvider.GetUtcNow().UtcDateTime, normalized, out var timeStepMatched, Window))
        {
            return null;
        }

        return timeStepMatched > lastAcceptedTimeStep ? timeStepMatched : null;
    }
}
