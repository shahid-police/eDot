using Elegance.Infrastructure.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Elegance.Tests.TestSupport;

public static class TestServices
{
    public static TokenService CreateTokenService(TimeProvider timeProvider) => new(Options.Create(new JwtOptions
    {
        Issuer = "elegance-api-test",
        Audience = "elegance-web-test",
        Key = "test-only-signing-key-at-least-32-characters-long",
        AccessTokenMinutes = 15,
    }), timeProvider);

    // Ephemeral: keys live only in memory for this provider instance, never touch disk. Perfect
    // for isolated, parallel-safe tests and for simulating "the decryption key is gone" by simply
    // creating a second, unrelated provider instance.
    public static TwoFactorSecretProtector CreateSecretProtector() =>
        new(new EphemeralDataProtectionProvider());

    public static FakeTimeProvider CreateFakeTimeProvider() => new(DateTimeOffset.UtcNow);

    public static TotpService CreateTotpService(FakeTimeProvider timeProvider) => new(timeProvider);
}
