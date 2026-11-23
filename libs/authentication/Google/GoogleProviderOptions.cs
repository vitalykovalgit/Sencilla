namespace Sencilla.Authentication;

/// <summary>
/// Google configuration, bound from <c>"Authentication:Google"</c>. Defaults target Google's production OIDC
/// endpoints. The verifier members configure the <c>id_token</c> verifier; the <see cref="RedirectSignInOptions"/>
/// ones (<c>ClientId</c>, <c>ClientSecret</c>, <c>RedirectSignIn</c>, <c>SignInFailedUrl</c>) ASP.NET's Google
/// redirect handler on <c>/signin-google</c>.
/// </summary>
public sealed class GoogleProviderOptions : ProviderVerifierOptions
{
    public GoogleProviderOptions()
    {
        MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";
        Issuers = ["https://accounts.google.com", "accounts.google.com"];
    }
}
