using System.ComponentModel.DataAnnotations;
using Elegance.Application.Customers;

namespace Elegance.Application.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; init; }
}

public class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public class AuthSession
{
    public required string AccessToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
    public required CustomerDto Customer { get; init; }
}
