namespace Sencilla.Authentication;

/// <summary>
/// Google configuration, bound from <c>"Authentication:Google"</c>. Defaults target Google's production OIDC
/// endpoints. The base members configure the <c>id_token</c> verifier; <see cref="RedirectSignIn"/> also
/// turns on ASP.NET's Google redirect handler.
/// </summary>
public sealed class GoogleProviderOptions : ProviderVerifierOptions
{
    public GoogleProviderOptions()
    {
        MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";
        Issuers = ["https://accounts.google.com", "accounts.google.com"];
    }

    /// <summary>OAuth client secret for the redirect sign-in. Required when <see cref="RedirectSignIn"/> is on.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Register ASP.NET's Google handler (<c>/signin-google</c>) signing into the host's default sign-in
    /// scheme; the identity Google returns is resolved to an account by
    /// <see cref="IExternalAuthService.ResolveAccount"/> before the session exists.
    /// Needs <see cref="ProviderVerifierOptions.ClientId"/> and <see cref="ClientSecret"/>.
    /// </summary>
    public bool RedirectSignIn { get; set; }

    /// <summary>
    /// Where a refused or failed redirect sign-in lands, as <c>{SignInFailedUrl}?error={code}</c>: the
    /// resolver's refusal code (e.g. <c>account-not-active</c>, <c>email-not-verified</c>) or
    /// <c>google-failed</c>. When null, the failure surfaces as the framework's error.
    /// </summary>
    public string? SignInFailedUrl { get; set; }
}
