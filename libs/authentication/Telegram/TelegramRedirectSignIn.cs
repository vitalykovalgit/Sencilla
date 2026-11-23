namespace Sencilla.Authentication;

/// <summary>
/// Log In With Telegram on ASP.NET's generic OpenID Connect handler: authorization code + PKCE, the claims read from
/// the <c>id_token</c> alone (Telegram has no user-info endpoint), and how they read as an
/// <see cref="ExternalIdentity"/> for the shared binding (<see cref="AuthenticationConfig.AddRedirectSignIn"/>).
/// Endpoints, scopes and claim names are those of <c>https://core.telegram.org/bots/telegram-login</c> and its
/// discovery document; what that page does not state is marked UNVERIFIED and kept to the constants below.
/// </summary>
internal static class TelegramRedirectSignIn
{
    private const string Authority = "https://oauth.telegram.org";
    private const string PhoneScope = "phone";
    private const string BotAccessScope = "telegram:bot_access";

    // UNVERIFIED: the page lists no claim for the granted bot access. It is read from the token response's standard
    // `scope` (RFC 6749 §5.1), copied onto the principal under this claim; when Telegram omits it, the bot is taken as
    // refused — the safe side: the user connects notifications by hand.
    private const string GrantedScopesClaim = "scope";

    public static void Configure(OpenIdConnectOptions options, TelegramProviderOptions telegram)
    {
        options.Authority = Authority;
        options.ClientId = telegram.ClientId!;
        options.ClientSecret = telegram.ClientSecret!;
        options.CallbackPath = "/signin-telegram";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.MapInboundClaims = false;
        options.Scope.Add(PhoneScope);
        options.Scope.Add(BotAccessScope);
        // UNVERIFIED whether Telegram echoes a nonce on the redirect flow (the page documents one for its JS library
        // only); the code is redeemed over the back channel with PKCE and the secret, so none is asked for.
        options.ProtocolValidator.RequireNonce = false;
        // Telegram has no remote sign-out; left on, /signout-oidc would sign anyone out on a cross-site GET.
        options.RemoteSignOutPath = default;
        // Telegram's edge answers gzip unasked (its JWKS about every other time); the handler's back channel — the
        // discovery document, the JWKS and the code exchange all go through it — must decompress it.
        options.BackchannelHttpHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        options.Events.OnTokenValidated = context =>
        {
            if (context.TokenEndpointResponse?.Scope is { Length: > 0 } granted)
                context.Principal!.Identities.First().AddClaim(new Claim(GrantedScopesClaim, granted));
            return Task.CompletedTask;
        };
    }

    public static ExternalIdentity ReadIdentity(ClaimsPrincipal telegram)
    {
        // A digit string without the "+", e.g. "971577777777".
        long? phone = long.TryParse(telegram.FindFirstValue("phone_number")?.TrimStart('+'), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
        return new ExternalIdentity(
            Provider: "telegram",
            Subject: telegram.FindFirstValue("sub")
                ?? throw new InvalidOperationException("Telegram returned no subject, so the sign-in cannot be bound to an account."),
            Email: null,
            EmailVerified: false,
            Name: telegram.FindFirstValue("name"),
            Picture: telegram.FindFirstValue("picture"),
            RawClaims: FindChatTheBotMayWriteTo(telegram) is { } chat ? new Dictionary<string, string> { [TelegramRawClaims.BotChatId] = chat } : null,
            GivenName: telegram.FindFirstValue("given_name"),
            FamilyName: telegram.FindFirstValue("family_name"),
            Phone: phone,
            // Telegram shares only the account's own, verified number.
            PhoneVerified: phone is not null);
    }

    /// <summary>The user's Telegram id (the numeric <c>id</c> claim, not <c>sub</c>) — the private chat our bot may write to — when they let it.</summary>
    private static string? FindChatTheBotMayWriteTo(ClaimsPrincipal telegram)
    {
        var mayBotWrite = telegram.FindFirstValue(GrantedScopesClaim)?.Split(' ').Contains(BotAccessScope) == true;
        return mayBotWrite ? telegram.FindFirstValue("id") : null;
    }
}
