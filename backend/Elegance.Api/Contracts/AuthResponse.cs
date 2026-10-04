using Elegance.Application.Customers;

namespace Elegance.Api.Contracts;

public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, CustomerDto Customer);
