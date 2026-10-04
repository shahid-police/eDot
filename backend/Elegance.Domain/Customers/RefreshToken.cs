namespace Elegance.Domain.Customers;

public class RefreshToken
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
