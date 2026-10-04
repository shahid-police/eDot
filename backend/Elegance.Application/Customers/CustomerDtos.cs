using System.ComponentModel.DataAnnotations;

namespace Elegance.Application.Customers;

public class CustomerDto
{
    public required long Id { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class AddressDto
{
    public required long Id { get; init; }
    public string? Label { get; init; }
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string Province { get; init; }
    public string? PostalCode { get; init; }
    public required string Country { get; init; }
    public required bool IsDefaultShipping { get; init; }
    public required bool IsDefaultBilling { get; init; }
}

public class AddressRequest
{
    [StringLength(50)]
    public string? Label { get; init; }

    [Required, StringLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required, StringLength(20)]
    public string Phone { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string AddressLine1 { get; init; } = string.Empty;

    [StringLength(200)]
    public string? AddressLine2 { get; init; }

    [Required, StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Province { get; init; } = string.Empty;

    [StringLength(20)]
    public string? PostalCode { get; init; }

    [StringLength(2, MinimumLength = 2)]
    public string? Country { get; init; }

    public bool IsDefaultShipping { get; init; }
    public bool IsDefaultBilling { get; init; }
}
