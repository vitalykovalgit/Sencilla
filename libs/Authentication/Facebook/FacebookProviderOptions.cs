namespace Sencilla.Authentication;

/// <summary>Facebook (Limited Login) verifier configuration. Audiences are the Facebook app ids.</summary>
public sealed class FacebookProviderOptions : ProviderVerifierOptions
{
    public FacebookProviderOptions()
    {
        MetadataAddress = "https://www.facebook.com/.well-known/openid-configuration/";
        Issuers = ["https://www.facebook.com", "https://facebook.com"];
    }
}
