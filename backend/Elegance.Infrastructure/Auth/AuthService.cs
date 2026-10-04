using Elegance.Application.Auth;
using Elegance.Application.Common;
using Elegance.Application.Customers;
using Elegance.Domain.Customers;
using Elegance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Auth;

public class AuthService(EleganceDbContext dbContext, TokenService tokenService) : IAuthService
{
    private static readonly string InvalidCredentialsMessage = "Invalid email or password.";
    private static readonly string InvalidSessionMessage = "Your session has expired. Please sign in again.";

    private readonly PasswordHasher<Customer> _passwordHasher = new();

    public async Task<AuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await dbContext.Customers.AnyAsync(c => c.Email == email, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "An account with this email already exists.");
        }

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        customer.PasswordHash = _passwordHasher.HashPassword(customer, request.Password);

        dbContext.Customers.Add(customer);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await dbContext.Customers.AnyAsync(c => c.Email == email, cancellationToken))
            {
                throw new ApplicationFailure(FailureKind.Conflict, "An account with this email already exists.");
            }
            throw;
        }

        return await IssueSessionAsync(customer, cancellationToken);
    }

    public async Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        if (customer is null
            || !customer.IsActive
            || _passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, InvalidCredentialsMessage);
        }

        return await IssueSessionAsync(customer, cancellationToken);
    }

    public async Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, InvalidSessionMessage);
        }

        var now = DateTime.UtcNow;

        if (stored.RevokedAt is not null)
        {
            await dbContext.RefreshTokens
                .Where(r => r.CustomerId == stored.CustomerId && r.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, now), cancellationToken);
            throw new ApplicationFailure(FailureKind.Unauthorized, InvalidSessionMessage);
        }

        if (!stored.IsActive(now) || !stored.Customer.IsActive)
        {
            throw new ApplicationFailure(FailureKind.Unauthorized, InvalidSessionMessage);
        }

        var replacement = TokenService.CreateRefreshToken();
        var replacementHash = TokenService.HashRefreshToken(replacement);
        var replacementExpiresAt = tokenService.RefreshTokenExpiry();

        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = replacementHash;
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            CustomerId = stored.CustomerId,
            TokenHash = replacementHash,
            ExpiresAt = replacementExpiresAt,
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildSession(stored.Customer, replacement, replacementExpiresAt);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var stored = await dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthSession> IssueSessionAsync(Customer customer, CancellationToken cancellationToken)
    {
        var refreshToken = TokenService.CreateRefreshToken();
        var refreshExpiresAt = tokenService.RefreshTokenExpiry();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            CustomerId = customer.Id,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildSession(customer, refreshToken, refreshExpiresAt);
    }

    private AuthSession BuildSession(Customer customer, string refreshToken, DateTime refreshExpiresAt)
    {
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(customer);

        return new AuthSession
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            Customer = new CustomerDto
            {
                Id = customer.Id,
                Email = customer.Email,
                Phone = customer.Phone,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                CreatedAt = customer.CreatedAt,
            },
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
