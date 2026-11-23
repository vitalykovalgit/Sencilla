namespace Sencilla.Authentication;

/// <summary>
/// What Google's redirect sign-in adds to the shared binding (<see cref="AuthenticationConfig.AddRedirectSignIn"/>):
/// the handler's client and claims, and how its principal reads as an <see cref="ExternalIdentity"/>.
/// </summary>
internal static class GoogleRedirectSignIn
{
    public static void Configure(GoogleOptions options, GoogleProviderOptions google)
    {
        options.ClientId = google.ClientId!;
        options.ClientSecret = google.ClientSecret!;
        options.Scope.Add("profile");
        // Google's handler maps neither from the user-info answer; the identity needs both.
        options.ClaimActions.MapJsonKey(AuthClaims.Picture, "picture");
        options.ClaimActions.MapJsonKey(AuthClaims.EmailVerified, "email_verified");
    }

    public static ExternalIdentity ReadIdentity(ClaimsPrincipal google) => new(
        Provider: "google",
        Subject: google.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Google returned no subject, so the sign-in cannot be bound to an account."),
        Email: google.FindFirstValue(ClaimTypes.Email),
        EmailVerified: bool.TryParse(google.FindFirstValue(AuthClaims.EmailVerified), out var verified) && verified,
        Name: google.FindFirstValue(ClaimTypes.Name),
        Picture: google.FindFirstValue(AuthClaims.Picture),
        GivenName: google.FindFirstValue(ClaimTypes.GivenName),
        FamilyName: google.FindFirstValue(ClaimTypes.Surname));
}
