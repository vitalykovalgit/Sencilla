namespace Sencilla.Component.Users.Auth;

/// <summary>
/// 
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}