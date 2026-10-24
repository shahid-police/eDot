using Elegance.Infrastructure.Auth;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Elegance.Tests.Auth;

public class TwoFactorSecretProtectorTests
{
    [Fact]
    public void Encrypt_then_Decrypt_round_trips_the_original_secret()
    {
        var protector = new TwoFactorSecretProtector(new EphemeralDataProtectionProvider());
        var secret = new byte[] { 1, 2, 3, 4, 5, 250, 251, 252 };

        var encrypted = protector.Encrypt(secret);
        var decrypted = protector.Decrypt(encrypted);

        Assert.Equal(secret, decrypted);
    }

    [Fact]
    public void Encrypt_output_never_contains_the_plaintext_secret()
    {
        var protector = new TwoFactorSecretProtector(new EphemeralDataProtectionProvider());
        var secret = System.Text.Encoding.UTF8.GetBytes("a-very-identifiable-secret-marker");

        var encrypted = protector.Encrypt(secret);

        Assert.DoesNotContain("a-very-identifiable-secret-marker", encrypted);
        Assert.DoesNotContain(Convert.ToBase64String(secret), encrypted);
    }

    [Fact]
    public void Decrypt_fails_securely_when_the_key_ring_is_unavailable_rather_than_falling_back_to_plaintext()
    {
        // Simulates total loss of the encryption key: a second, unrelated provider cannot
        // decrypt what the first one encrypted. There is no plaintext fallback path.
        var protectorA = new TwoFactorSecretProtector(new EphemeralDataProtectionProvider());
        var protectorB = new TwoFactorSecretProtector(new EphemeralDataProtectionProvider());
        var encrypted = protectorA.Encrypt([9, 9, 9]);

        Assert.ThrowsAny<CryptographicException>(() => protectorB.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_rejects_tampered_ciphertext()
    {
        var protector = new TwoFactorSecretProtector(new EphemeralDataProtectionProvider());
        var encrypted = protector.Encrypt([1, 2, 3]);
        var tampered = encrypted[..^4] + "abcd";

        Assert.ThrowsAny<Exception>(() => protector.Decrypt(tampered));
    }
}
