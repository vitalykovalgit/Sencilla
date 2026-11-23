namespace Sencilla.Authentication;

/// <summary>
/// Apple configuration, bound from <c>"Authentication:Apple"</c>. The verifier members configure the <c>id_token</c>
/// verifier (audiences are the Services ID / bundle ids); the redirect sign-in takes <c>ClientId</c> = the Services ID
/// and, instead of a client secret, the key it signs one with: <see cref="TeamId"/>, <see cref="KeyId"/> and
/// <see cref="PrivateKey"/>.
/// </summary>
public sealed class AppleProviderOptions : ProviderVerifierOptions
{
    public AppleProviderOptions()
    {
        MetadataAddress = "https://appleid.apple.com/.well-known/openid-configuration";
        Issuers = ["https://appleid.apple.com"];
    }

    /// <summary>The Apple Developer Team ID — the issuer of the client secret the redirect sign-in signs.</summary>
    public string? TeamId { get; set; }

    /// <summary>The id of the Sign in with Apple key.</summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// The CONTENT of that key's <c>.p8</c> file (PEM, <c>-----BEGIN PRIVATE KEY-----</c> included), from the
    /// deployment's secrets — never a path to a file in the repository.
    /// </summary>
    public string? PrivateKey { get; set; }

    protected override IEnumerable<string> FindMissingRedirectSettings()
        => NamesOfBlank((nameof(ClientId), ClientId), (nameof(TeamId), TeamId), (nameof(KeyId), KeyId), (nameof(PrivateKey), PrivateKey));
}
