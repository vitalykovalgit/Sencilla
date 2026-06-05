namespace Sencilla.Authentication;

/// <summary>
/// Bearer-validation configuration for a resource app. Set <see cref="Authority"/> for the SSO/IDP
/// path (validate via published JWKS — no secret held) or <see cref="SigningKey"/> for the embedded
/// HMAC path.
/// </summary>
public sealed class ClientAuthOptions
{
    /// <summary>IDP base URL; JWT-bearer fetches its metadata + JWKS automatically.</summary>
    public string? Authority { get; set; }

    /// <summary>Explicit discovery document URL (overrides <see cref="Authority"/>).</summary>
    public string? MetadataAddress { get; set; }

    /// <summary>Symmetric key for validating embedded HMAC tokens. Mutually exclusive with <see cref="Authority"/>.</summary>
    public string? SigningKey { get; set; }

    public string? Issuer { get; set; }

    public IList<string> Audiences { get; set; } = [];

    public bool RequireHttpsMetadata { get; set; } = true;
}
