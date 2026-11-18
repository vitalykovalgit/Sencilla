namespace Sencilla.Authentication;

/// <summary>
/// ASP.NET's Google redirect handler bound to the account graph: the identity Google returns is resolved by
/// <see cref="IExternalAuthService.ResolveAccount"/> BEFORE the session exists, and the ticket's principal is
/// the one <see cref="IClaimsPrincipalFactory"/> builds for that account, not Google's identity. A refusal is
/// thrown from the ticket event — the OAuth handler ignores a <c>Fail</c> set there and would sign the user in
/// anyway — and lands on
/// <see cref="GoogleProviderOptions.SignInFailedUrl"/> through <c>OnRemoteFailure</c>.
/// </summary>
internal static class GoogleRedirectSignIn
{
    public static void Configure(GoogleOptions options, GoogleProviderOptions google)
    {
        options.ClientId = google.ClientId!;
        options.ClientSecret = google.ClientSecret!;
        options.Scope.Add("profile");
        options.Events.OnCreatingTicket = BindTicketToAccount;
        if (google.SignInFailedUrl is { } failedUrl)
            options.Events.OnRemoteFailure = context => RedirectToSignInFailed(context, failedUrl);
    }

    private static async Task BindTicketToAccount(OAuthCreatingTicketContext c)
    {
        if (c.Identity is not { } identity || identity.FindFirst(ClaimTypes.NameIdentifier)?.Value is not { } subject)
            throw new InvalidOperationException("Google returned no subject, so the sign-in cannot be bound to an account.");

        var picture = c.User.TryGetProperty("picture", out var p) ? p.GetString() : null;
        var verified = c.User.TryGetProperty("email_verified", out var v) && v.ValueKind == JsonValueKind.True;
        var email = identity.FindFirst(ClaimTypes.Email)?.Value;
        var name = identity.FindFirst(ClaimTypes.Name)?.Value;
        var given = identity.FindFirst(ClaimTypes.GivenName)?.Value;
        var surname = identity.FindFirst(ClaimTypes.Surname)?.Value;

        var services = c.HttpContext.RequestServices;
        var user = await services.GetRequiredService<IExternalAuthService>().ResolveAccount(
            new ExternalIdentity("google", subject, email, verified, name, picture, GivenName: given, FamilyName: surname), c.HttpContext.RequestAborted);

        // The session is the library's own principal for the account (its id as `sub`), not Google's identity.
        c.Principal = await services.GetRequiredService<IClaimsPrincipalFactory>().Create(user, scopes: null, c.HttpContext.RequestAborted);

        // A Google sign-in survives the browser closing.
        c.Properties.IsPersistent = true;
    }

    private static Task RedirectToSignInFailed(RemoteFailureContext c, string failedUrl)
    {
        var code = c.Failure is UnauthorizedException refused ? refused.Message : "google-failed";
        c.Response.Redirect(QueryHelpers.AddQueryString(failedUrl, "error", code));
        c.HandleResponse();
        return Task.CompletedTask;
    }
}
