namespace Sencilla.Authentication;

/// <summary>
/// Base configuration for an external provider verifier. Each provider ships defaults for
/// <see cref="MetadataAddress"/> and <see cref="Issuers"/>; the host supplies the accepted
/// <see cref="Audiences"/> (the provider client ids for each platform — web, iOS, Android).
/// </summary>
public abstract class ProviderVerifierOptions
{
    /// <summary>OpenID Connect discovery document URL for the provider.</summary>
    public string MetadataAddress { get; set; } = default!;

    /// <summary>Accepted token audiences (client ids). A token whose <c>aud</c> is not listed is rejected.</summary>
    public IList<string> Audiences { get; set; } = new List<string>();

    /// <summary>Convenience single client id (e.g. Google). Folded into <see cref="Audiences"/> at registration.</summary>
    public string? ClientId { get; set; }

    /// <summary>Convenience single app id (e.g. Facebook/Instagram). Folded into <see cref="Audiences"/> at registration.</summary>
    public string? AppId { get; set; }

    /// <summary>Accepted issuers. When null, the issuer from the discovery document is used.</summary>
    public IList<string>? Issuers { get; set; }

    /// <summary>
    /// Optional pre-built configuration manager. When set, it is used instead of fetching the
    /// discovery document from <see cref="MetadataAddress"/> (used to inject static JWKS in tests).
    /// </summary>
    internal IConfigurationManager<OpenIdConnectConfiguration>? ConfigurationManager { get; set; }
}
