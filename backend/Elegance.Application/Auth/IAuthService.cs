namespace Elegance.Application.Auth;

public interface IAuthService
{
    Task<AuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
}
