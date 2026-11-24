namespace Elegance.Domain.Vendors;

public enum VendorTwoFactorStatus
{
    Pending,
    Active,
}

// Mirrors Elegance.Domain.Admin.AdminTwoFactorCredential exactly, one-to-one with Vendor.
// Unlike admin 2FA (optional, opt-in from the dashboard), vendor 2FA is mandatory: a vendor
// cannot obtain a real portal session until this row reaches Active - see VendorAuthService.
public class VendorTwoFactorCredential
{
    public long VendorId { get; set; }
    public required string EncryptedSecret { get; set; }
    public VendorTwoFactorStatus Status { get; set; } = VendorTwoFactorStatus.Pending;
    public long LastAcceptedTimeStep { get; set; } = -1;
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? PendingExpiresAt { get; set; }

    public Vendor Vendor { get; set; } = null!;
}
