namespace Elegance.Application.Customers;

public interface ICustomerService
{
    Task<CustomerDto> GetProfileAsync(long customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressDto>> GetAddressesAsync(long customerId, CancellationToken cancellationToken = default);
    Task<AddressDto> AddAddressAsync(long customerId, AddressRequest request, CancellationToken cancellationToken = default);
    Task<AddressDto> UpdateAddressAsync(long customerId, long addressId, AddressRequest request, CancellationToken cancellationToken = default);
    Task DeleteAddressAsync(long customerId, long addressId, CancellationToken cancellationToken = default);
}
