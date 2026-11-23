namespace Sencilla.Authentication;

/// <summary>
/// What Facebook's redirect sign-in adds to the shared binding (<see cref="AuthenticationConfig.AddRedirectSignIn"/>):
/// the handler's app and fields, and how its principal reads as an <see cref="ExternalIdentity"/>. Facebook gives no
/// verified flag, so its address is never verified here — it goes to the account only through our own link.
/// </summary>
internal static class FacebookRedirectSignIn
{
    public static void Configure(FacebookOptions options, FacebookProviderOptions facebook)
    {
        options.AppId = facebook.ClientId!;
        options.AppSecret = facebook.ClientSecret!;
        options.Fields.Add("picture");
        // The Graph answers "picture": { "data": { "url": … } }.
        options.ClaimActions.MapCustomJson(AuthClaims.Picture, user =>
            user.TryGetProperty("picture", out var picture) && picture.TryGetProperty("data", out var data)
                && data.TryGetProperty("url", out var url) ? url.GetString() : null);
    }

    public static ExternalIdentity ReadIdentity(ClaimsPrincipal facebook) => new(
        Provider: "facebook",
        Subject: facebook.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Facebook returned no id, so the sign-in cannot be bound to an account."),
        Email: facebook.FindFirstValue(ClaimTypes.Email),
        EmailVerified: false,
        Name: facebook.FindFirstValue(ClaimTypes.Name),
        Picture: facebook.FindFirstValue(AuthClaims.Picture),
        GivenName: facebook.FindFirstValue(ClaimTypes.GivenName),
        FamilyName: facebook.FindFirstValue(ClaimTypes.Surname));
}
