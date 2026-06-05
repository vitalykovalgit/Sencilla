namespace Sencilla.Authentication;

/// <summary>Apple verifier configuration. Audiences are the app's Service ID / bundle ids.</summary>
public sealed class AppleProviderOptions : ProviderVerifierOptions
{
    public AppleProviderOptions()
    {
        MetadataAddress = "https://appleid.apple.com/.well-known/openid-configuration";
        Issuers = ["https://appleid.apple.com"];
    }
}
