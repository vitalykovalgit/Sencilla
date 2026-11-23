namespace Sencilla.Authentication;

/// <summary>
/// ASP.NET's generic OAuth handler with the two steps Instagram does its own way: the code exchange answers its token
/// inside <c>data</c> (<see cref="InstagramRedirectSignIn.FirstEntryOf"/>), and the profile is a <c>/me</c> call on
/// the Graph host with the token as a query parameter.
/// </summary>
internal sealed class InstagramHandler(IOptionsMonitor<OAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : OAuthHandler<OAuthOptions>(options, logger, encoder)
{
    protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
    {
        var tokens = await base.ExchangeCodeAsync(context);
        if (tokens.Error is not null || !string.IsNullOrEmpty(tokens.AccessToken))
            return tokens;

        using (tokens)
            return OAuthTokenResponse.Success(JsonDocument.Parse(InstagramRedirectSignIn.FirstEntryOf(tokens.Response!.RootElement).GetRawText()));
    }

    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
    {
        var address = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, new Dictionary<string, string?>
        {
            ["fields"] = InstagramRedirectSignIn.ProfileFields,
            ["access_token"] = tokens.AccessToken,
        });
        using var response = await Backchannel.GetAsync(address, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Instagram's profile answered {(int)response.StatusCode}.");

        using var profile = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted));
        var context = new OAuthCreatingTicketContext(
            new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, InstagramRedirectSignIn.FirstEntryOf(profile.RootElement));
        context.RunClaimActions();
        await Events.CreatingTicket(context);
        return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
    }
}
