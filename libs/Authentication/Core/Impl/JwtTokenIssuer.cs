namespace Sencilla.Authentication;

/// <summary>
/// Embedded HMAC <see cref="ITokenIssuer"/>: signs a short-lived access JWT from the principal and
/// issues a rotating refresh token. For single-process apps only — see the package README.
/// </summary>
public sealed class JwtTokenIssuer(IOptions<JwtIssuerOptions> options, RefreshTokenService refresh) : ITokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtIssuerOptions _options = options.Value;

    public async Task<TokenResponse> Issue(ClaimsPrincipal principal, CancellationToken token = default)
    {
        var (accessToken, expires) = MintAccessToken(principal);
        var refreshToken = await refresh.Issue(SubjectId(principal), cancellation: token);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expires,
        };
    }

    public Task<TokenResponse> IssueAccessToken(ClaimsPrincipal principal, CancellationToken token = default)
    {
        var (accessToken, expires) = MintAccessToken(principal);
        return Task.FromResult(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = null,
            ExpiresAt = expires,
        });
    }

    private (string AccessToken, DateTime ExpiresAt) MintAccessToken(ClaimsPrincipal principal)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = principal.Identity as ClaimsIdentity ?? new ClaimsIdentity(principal.Claims),
            Expires = expires,
            SigningCredentials = credentials,
        };

        return (Handler.CreateToken(descriptor), expires);
    }

    private static Guid SubjectId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst(AuthClaims.Subject)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
