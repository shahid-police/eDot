using Elegance.Domain.Customers;

namespace Elegance.Domain.Carts;

public class Cart
{
    public long Id { get; set; }

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? GuestTokenHash { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = [];
}
