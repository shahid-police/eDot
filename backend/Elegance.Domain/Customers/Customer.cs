namespace Elegance.Domain.Customers;

public class Customer
{
    public long Id { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CustomerAddress> Addresses { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
