using Elegance.Infrastructure.Admin;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Inventory;
using Elegance.Infrastructure.Notifications;
using Elegance.Infrastructure.Vendors;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Elegance.Tests.TestSupport;

// Mirrors AdminAuthTestContext: wires VendorAuthService, VendorCatalogService, and the admin-side
// vendor services against a shared in-memory SQLite db, a shared fake clock, and a shared
// ephemeral secret protector.
public sealed class VendorAuthTestContext : IDisposable
{
    public SqliteTestDb Db { get; } = new();
    public FakeTimeProvider Clock { get; } = TestServices.CreateFakeTimeProvider();
    public TotpService TotpService { get; }
    public TwoFactorSecretProtector SecretProtector { get; } = TestServices.CreateSecretProtector();
    public TokenService TokenService { get; }
    public FakeFileStorage FileStorage { get; } = new();
    public AuditLogService AuditLog { get; }
    public VendorNotificationOptions NotificationOptions { get; } = new();
    public VendorNotificationQueuer NotificationQueuer { get; }
    public VendorAuthService AuthService { get; }
    public AdminCatalogService AdminCatalogService { get; }
    public VendorCatalogService CatalogService { get; }
    public VendorOrderService OrderService { get; }
    public AdminVendorService AdminVendorService { get; }
    public AdminVendorSubmissionService AdminSubmissionService { get; }
    public AdminMediaService AdminMediaService { get; }

    public VendorAuthTestContext()
    {
        TotpService = TestServices.CreateTotpService(Clock);
        TokenService = TestServices.CreateTokenService(Clock);
        AuditLog = new AuditLogService(Db.Context);
        NotificationQueuer = new VendorNotificationQueuer(Db.Context, Options.Create(NotificationOptions));
        AuthService = new VendorAuthService(Db.Context, TokenService, TotpService, SecretProtector);
        AdminCatalogService = new AdminCatalogService(Db.Context, AuditLog, new RestockDetectionService(Db.Context));
        CatalogService = new VendorCatalogService(Db.Context, FileStorage, NotificationQueuer, AdminCatalogService);
        OrderService = new VendorOrderService(Db.Context);
        AdminVendorService = new AdminVendorService(Db.Context, AuditLog);
        AdminMediaService = new AdminMediaService(Db.Context, FileStorage, AuditLog);
        AdminSubmissionService = new AdminVendorSubmissionService(Db.Context, FileStorage, AdminMediaService, AuditLog, NotificationQueuer);
    }

    public void Dispose() => Db.Dispose();
}
