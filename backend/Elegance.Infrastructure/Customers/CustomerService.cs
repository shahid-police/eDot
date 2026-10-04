using Elegance.Application.Common;
using Elegance.Application.Customers;
using Elegance.Domain.Customers;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Customers;

public class CustomerService(EleganceDbContext dbContext) : ICustomerService
{
    public async Task<CustomerDto> GetProfileAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw NotFound();

        return ToDto(customer);
    }

    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var addresses = await dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return addresses.Select(ToDto).ToList();
    }

    public async Task<AddressDto> AddAddressAsync(long customerId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var address = new CustomerAddress
        {
            CustomerId = customerId,
            FullName = string.Empty,
            Phone = string.Empty,
            AddressLine1 = string.Empty,
            City = string.Empty,
            Province = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        ApplyRequest(address, request);

        await ClearDefaultsAsync(customerId, request, excludeAddressId: null, cancellationToken);
        dbContext.CustomerAddresses.Add(address);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(address);
    }

    public async Task<AddressDto> UpdateAddressAsync(long customerId, long addressId, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await FindOwnedAddressAsync(customerId, addressId, cancellationToken);

        ApplyRequest(address, request);
        address.UpdatedAt = DateTime.UtcNow;

        await ClearDefaultsAsync(customerId, request, excludeAddressId: addressId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(address);
    }

    public async Task DeleteAddressAsync(long customerId, long addressId, CancellationToken cancellationToken = default)
    {
        var address = await FindOwnedAddressAsync(customerId, addressId, cancellationToken);
        dbContext.CustomerAddresses.Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustomerAddress> FindOwnedAddressAsync(long customerId, long addressId, CancellationToken cancellationToken)
    {
        return await dbContext.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId, cancellationToken)
            ?? throw NotFound();
    }

    private async Task ClearDefaultsAsync(long customerId, AddressRequest request, long? excludeAddressId, CancellationToken cancellationToken)
    {
        if (request.IsDefaultShipping)
        {
            await dbContext.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsDefaultShipping && a.Id != excludeAddressId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefaultShipping, false), cancellationToken);
        }

        if (request.IsDefaultBilling)
        {
            await dbContext.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsDefaultBilling && a.Id != excludeAddressId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefaultBilling, false), cancellationToken);
        }
    }

    private static void ApplyRequest(CustomerAddress address, AddressRequest request)
    {
        address.Label = request.Label?.Trim();
        address.FullName = request.FullName.Trim();
        address.Phone = request.Phone.Trim();
        address.AddressLine1 = request.AddressLine1.Trim();
        address.AddressLine2 = request.AddressLine2?.Trim();
        address.City = request.City.Trim();
        address.Province = request.Province.Trim();
        address.PostalCode = request.PostalCode?.Trim();
        address.Country = string.IsNullOrWhiteSpace(request.Country) ? "PK" : request.Country.Trim().ToUpperInvariant();
        address.IsDefaultShipping = request.IsDefaultShipping;
        address.IsDefaultBilling = request.IsDefaultBilling;
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Email = customer.Email,
        Phone = customer.Phone,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        CreatedAt = customer.CreatedAt,
    };

    private static AddressDto ToDto(CustomerAddress address) => new()
    {
        Id = address.Id,
        Label = address.Label,
        FullName = address.FullName,
        Phone = address.Phone,
        AddressLine1 = address.AddressLine1,
        AddressLine2 = address.AddressLine2,
        City = address.City,
        Province = address.Province,
        PostalCode = address.PostalCode,
        Country = address.Country,
        IsDefaultShipping = address.IsDefaultShipping,
        IsDefaultBilling = address.IsDefaultBilling,
    };

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
