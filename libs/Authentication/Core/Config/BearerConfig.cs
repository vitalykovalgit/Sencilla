namespace Sencilla.Authentication;

/// <summary>
/// Configures incoming bearer-token validation (<c>AcceptBearer</c>). Two modes: set
/// <see cref="Authority"/> / <see cref="MetadataAddress"/> to validate an external IDP's tokens via
/// its published JWKS, or leave them unset to validate the embedded HMAC tokens this app issues —
/// in which case the signing key / issuer / audience are reused from <c>UseJwtToken</c> unless
/// overridden here.
/// </summary>
public sealed class BearerConfig
{
    internal string? AuthorityValue { get; private set; }
    internal string? MetadataAddressValue { get; private set; }
    internal string? SigningKeyValue { get; private set; }
    internal string? IssuerValue { get; private set; }
    internal List<string> AudiencesValue { get; } = [];
    internal bool? RequireHttpsValue { get; private set; }

    /// <summary>IDP base URL; JWT-bearer fetches metadata + JWKS automatically.</summary>
    public BearerConfig Authority(string authority)
    {
        AuthorityValue = authority;
        return this;
    }

    /// <summary>Explicit discovery-document URL (overrides <see cref="Authority"/>).</summary>
    public BearerConfig MetadataAddress(string url)
    {
        MetadataAddressValue = url;
        return this;
    }

    /// <summary>Symmetric key for the embedded HMAC path (defaults to the issuer's key).</summary>
    public BearerConfig SigningKey(string key)
    {
        SigningKeyValue = key;
        return this;
    }

    public BearerConfig Issuer(string issuer)
    {
        IssuerValue = issuer;
        return this;
    }

    public BearerConfig Audience(string audience)
    {
        AudiencesValue.Add(audience);
        return this;
    }

    public BearerConfig RequireHttps(bool require = true)
    {
        RequireHttpsValue = require;
        return this;
    }
}
