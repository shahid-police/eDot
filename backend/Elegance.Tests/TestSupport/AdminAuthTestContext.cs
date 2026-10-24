using Elegance.Infrastructure.Admin;
using Elegance.Infrastructure.Auth;
using Microsoft.Extensions.Time.Testing;

namespace Elegance.Tests.TestSupport;

// Wires up AdminAuthService + AdminTwoFactorService against a shared in-memory SQLite db, a
// shared fake clock, and a shared (ephemeral) secret protector - mirroring how DI wires the real
// services, but fully isolated and deterministic.
public sealed class AdminAuthTestContext : IDisposable
{
    public SqliteTestDb Db { get; } = new();
    public FakeTimeProvider Clock { get; } = TestServices.CreateFakeTimeProvider();
    public TotpService TotpService { get; }
    public TwoFactorSecretProtector SecretProtector { get; } = TestServices.CreateSecretProtector();
    public TokenService TokenService { get; }
    public AuditLogService AuditLog { get; }
    public AdminAuthService AuthService { get; }
    public AdminTwoFactorService TwoFactorService { get; }

    public AdminAuthTestContext()
    {
        TotpService = TestServices.CreateTotpService(Clock);
        TokenService = TestServices.CreateTokenService(Clock);
        AuditLog = new AuditLogService(Db.Context);
        AuthService = new AdminAuthService(Db.Context, TokenService, TotpService, SecretProtector, AuditLog);
        TwoFactorService = new AdminTwoFactorService(Db.Context, TotpService, SecretProtector, AuditLog);
    }

    // A second AdminAuthService backed by its own DbContext/connection against the same
    // shared-cache database, so two "concurrent requests" are genuinely concurrent (one
    // DbContext instance must never be used from two threads at once).
    public AdminAuthService CreateAdditionalAuthService()
    {
        var context = Db.CreateAdditionalContext();
        return new AdminAuthService(context, TokenService, TotpService, SecretProtector, new AuditLogService(context));
    }

    public void Dispose() => Db.Dispose();
}
