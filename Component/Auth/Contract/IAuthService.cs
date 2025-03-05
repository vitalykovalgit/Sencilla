namespace Sencilla.Component.Users.Auth;

public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<TokenResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default
    );
}