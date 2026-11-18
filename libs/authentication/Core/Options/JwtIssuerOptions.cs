namespace Sencilla.Authentication;

/// <summary>
/// Configuration for the embedded HMAC issuer (<c>UseJwtToken</c>). Binds from the <c>"Jwt"</c> section
/// before the <c>UseJwtToken</c> lambda runs.
/// </summary>
public sealed class JwtIssuerOptions
{
    /// <summary>Symmetric signing key (HMAC-SHA256, at least 32 bytes). Single-process use only.</summary>
    public string SigningKey { get; set; } = default!;

    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public int AccessTokenLifetimeMinutes { get; set; } = 10;

    /// <summary>Rotating refresh-token policy and storage (also served as <c>IOptions&lt;RefreshTokenOptions&gt;</c>).</summary>
    public RefreshTokenOptions Refresh { get; } = new();
}
