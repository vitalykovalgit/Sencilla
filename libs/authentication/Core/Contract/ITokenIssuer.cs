namespace Sencilla.Authentication;

/// <summary>
/// Materializes a token pair from an authenticated principal for the token-API / embedded path.
/// The default implementation lives in <c>Sencilla.Authentication.Jwt</c>. The full OIDC host
/// (<c>Sencilla.Authentication.OpenIddict</c>) does not use this seam — it issues tokens inside
/// its own request pipeline.
/// </summary>
public interface ITokenIssuer
{
    Task<TokenResponse> Issue(ClaimsPrincipal principal, CancellationToken token = default);

    /// <summary>
    /// Issues only a signed access JWT — does not touch the refresh-token store. Use on the
    /// refresh endpoint where the caller supplies a pre-rotated refresh token from
    /// <c>RefreshTokenService.Redeem</c>.  <see cref="TokenResponse.RefreshToken"/> is null.
    /// </summary>
    Task<TokenResponse> IssueAccessToken(ClaimsPrincipal principal, CancellationToken token = default);
}
