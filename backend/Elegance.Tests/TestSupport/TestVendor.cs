using Elegance.Domain.Vendors;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Elegance.Tests.TestSupport;

public static class TestVendor
{
    public static async Task<Vendor> CreateAsync(
        EleganceDbContext db,
        string email = "vendor@test.local",
        string password = "Correct-Horse-Battery-Staple-1",
        string vendorCode = "VEN-0001",
        VendorStatus status = VendorStatus.Active)
    {
        var hasher = new PasswordHasher<Vendor>();
        var vendor = new Vendor
        {
            Email = email,
            PasswordHash = string.Empty,
            VendorCode = vendorCode,
            ShopName = "Test Shop",
            ContactPerson = "Test Owner",
            Phone = "+92-300-0000000",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        vendor.PasswordHash = hasher.HashPassword(vendor, password);

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        return vendor;
    }
}
