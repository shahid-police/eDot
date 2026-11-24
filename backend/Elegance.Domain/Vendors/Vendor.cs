namespace Elegance.Domain.Vendors;

public enum VendorStatus
{
    Active,
    Suspended,
    Deactivated,
}

public class Vendor
{
    public long Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string VendorCode { get; set; }
    public required string ShopName { get; set; }
    public required string ContactPerson { get; set; }
    public required string Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public VendorStatus Status { get; set; } = VendorStatus.Active;
    public string? AdminRemarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<VendorProductSubmission> Submissions { get; set; } = [];
}
