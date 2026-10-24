using Microsoft.AspNetCore.DataProtection;

namespace Elegance.Infrastructure.Auth;

// Thin wrapper over ASP.NET Core's Data Protection API (authenticated encryption, key ring kept
// outside the database - see Program.cs for where keys are persisted). Never stores or logs the
// plaintext secret; fails rather than falling back to plaintext if the key ring is unavailable.
public class TwoFactorSecretProtector
{
    private const string Purpose = "Elegance.Admin.TwoFactorSecret.v1";
    private readonly IDataProtector _protector;

    public TwoFactorSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Encrypt(byte[] secret) => _protector.Protect(Convert.ToBase64String(secret));

    public byte[] Decrypt(string encryptedSecret) => Convert.FromBase64String(_protector.Unprotect(encryptedSecret));
}
