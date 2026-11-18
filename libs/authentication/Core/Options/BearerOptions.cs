namespace Sencilla.Authentication;

/// <summary>
/// Incoming bearer-token validation (<c>AcceptBearer</c>). Two modes: set
/// <see cref="Authority"/> / <see cref="MetadataAddress"/> to validate an external IDP's tokens via
/// its published JWKS, or leave them unset to validate the embedded HMAC tokens this app issues —
/// in which case the signing key / issuer / audience are reused from <c>UseJwtToken</c> unless
/// set here.
/// </summary>
public sealed class BearerOptions
{
    /// <summary>IDP base URL; JWT-bearer fetches metadata + JWKS automatically.</summary>
    public string? Authority { get; set; }

    /// <summary>Explicit discovery-document URL (overrides <see cref="Authority"/>).</summary>
    public string? MetadataAddress { get; set; }

    /// <summary>Symmetric key for the embedded HMAC path (defaults to the issuer's key).</summary>
    public string? SigningKey { get; set; }

    /// <summary>Expected issuer (defaults to the issuer's).</summary>
    public string? Issuer { get; set; }

    /// <summary>Accepted audiences (default: the issuer's audience).</summary>
    public IList<string> Audiences { get; set; } = new List<string>();

    /// <summary>Require HTTPS for metadata (default: on outside Development).</summary>
    public bool? RequireHttps { get; set; }
}
