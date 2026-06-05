namespace Sencilla.Authentication;

/// <summary>Google verifier configuration. Defaults target Google's production OIDC endpoints.</summary>
public sealed class GoogleProviderOptions : ProviderVerifierOptions
{
    public GoogleProviderOptions()
    {
        MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";
        Issuers = ["https://accounts.google.com", "accounts.google.com"];
    }
}
