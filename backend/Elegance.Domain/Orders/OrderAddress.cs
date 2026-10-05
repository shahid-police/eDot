using Elegance.Domain.Orders.Enums;

namespace Elegance.Domain.Orders;

public class OrderAddress
{
    public long Id { get; set; }

    public long OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public AddressType Type { get; set; } = AddressType.Shipping;

    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string Province { get; set; }
    public string? PostalCode { get; set; }
    public required string Country { get; set; }
}
