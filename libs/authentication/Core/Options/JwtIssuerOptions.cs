namespace Sencilla.Authentication;

/// <summary>Configuration for the embedded HMAC issuer.</summary>
public sealed class JwtIssuerOptions
{
    /// <summary>Symmetric signing key (HMAC-SHA256, at least 32 bytes). Single-process use only.</summary>
    public string SigningKey { get; set; } = default!;

    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public int AccessTokenLifetimeMinutes { get; set; } = 10;
}
