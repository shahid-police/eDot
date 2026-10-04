namespace Elegance.Domain.Customers;

public class CustomerAddress
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string? Label { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string Province { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "PK";
    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
