namespace Elegance.Domain.Vendors;

// Mirrors Elegance.Domain.Customers.RefreshToken exactly (rotate-on-use, revoke-chain-on-reuse).
public class VendorRefreshToken
{
    public long Id { get; set; }
    public long VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
