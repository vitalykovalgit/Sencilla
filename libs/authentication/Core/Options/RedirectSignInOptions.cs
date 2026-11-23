namespace Sencilla.Authentication;

/// <summary>
/// What every provider's redirect sign-in is configured with, in one place: the client the provider issued, the
/// switch that registers its ASP.NET handler, and where a refused or failed sign-in lands. A provider adds only what
/// it needs beyond these (Apple's key, say) and names any further required setting in
/// <see cref="FindMissingRedirectSettings"/>.
/// </summary>
public abstract class RedirectSignInOptions
{
    /// <summary>
    /// The client id the provider issued — Google's client id, Facebook's App ID, Apple's Services ID, Telegram's bot
    /// id, Instagram's app id. A provider that also verifies <c>id_token</c>s folds it into the accepted audiences.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>The OAuth client secret the redirect sign-in redeems its code with.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Register the provider's ASP.NET redirect handler, signing into the host's default sign-in scheme. The identity
    /// the provider returns is resolved to an account by <see cref="IExternalAuthService.ResolveAccount"/> before the
    /// session exists; startup fails while a setting it needs is missing.
    /// </summary>
    public bool RedirectSignIn { get; set; }

    /// <summary>
    /// Where a refused or failed redirect sign-in lands, as <c>{SignInFailedUrl}?error={code}</c>: the resolver's
    /// refusal code (e.g. <c>account-not-active</c>, <c>email-not-verified</c>) or <c>&lt;provider&gt;-failed</c>. A
    /// connect lands back on its challenge's <c>RedirectUri</c> instead. When null, the failure surfaces as the
    /// framework's error.
    /// </summary>
    public string? SignInFailedUrl { get; set; }

    /// <summary>The settings <see cref="RedirectSignIn"/> needs that are not set; startup refuses while there is any.</summary>
    protected internal virtual IEnumerable<string> FindMissingRedirectSettings()
        => NamesOfBlank((nameof(ClientId), ClientId), (nameof(ClientSecret), ClientSecret));

    /// <summary>The names of the <paramref name="settings"/> whose value is blank.</summary>
    protected static IEnumerable<string> NamesOfBlank(params (string Name, string? Value)[] settings)
        => settings.Where(s => string.IsNullOrWhiteSpace(s.Value)).Select(s => s.Name);
}
