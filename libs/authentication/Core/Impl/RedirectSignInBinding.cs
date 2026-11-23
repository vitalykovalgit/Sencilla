namespace Sencilla.Authentication;

/// <summary>
/// The one binding of a provider's redirect handler to the account graph, whichever handler it is (OAuth, OpenID
/// Connect, Apple's): it owns the two events every remote handler has. <c>OnTicketReceived</c> resolves the identity
/// the provider's reader takes from the handler's principal through <see cref="IExternalAuthService.ResolveAccount"/>
/// BEFORE the session exists, and swaps the ticket's principal for the one <see cref="IClaimsPrincipalFactory"/>
/// builds for that account (its id as <c>sub</c>), persistent. A refusal redirects and ends the request there: a
/// <c>Fail</c> set on that event is ignored and the user would be signed in anyway. <c>OnRemoteFailure</c> turns what
/// failed at the provider (denied, correlation, code exchange) into <c>&lt;provider&gt;-failed</c>.
/// A sign-in lands on <see cref="RedirectSignInOptions.SignInFailedUrl"/>, a connect
/// (<see cref="AuthenticationPropertiesEx.ConnectToAccountOf"/>) back on its challenge's <c>RedirectUri</c>, each with
/// <c>error=&lt;code&gt;</c>; with nowhere to land the failure surfaces as the framework's error.
/// </summary>
internal static class RedirectSignInBinding
{
    private const string FrameworkFailureMessage = "An error was encountered while handling the remote login.";

    public static void Bind(RemoteAuthenticationOptions handler, string provider, string? signInFailedUrl, Func<ClaimsPrincipal, ExternalIdentity> readIdentity)
    {
        var failedCode = $"{provider}-failed";
        handler.Events.OnTicketReceived = context => SignInAsResolvedAccount(context, readIdentity, failedCode, signInFailedUrl);
        handler.Events.OnRemoteFailure = context =>
        {
            RedirectWithError(context, LandingOf(context.Properties, context.Properties?.RedirectUri, signInFailedUrl), failedCode);
            return Task.CompletedTask;
        };
    }

    private static async Task SignInAsResolvedAccount(
        TicketReceivedContext context, Func<ClaimsPrincipal, ExternalIdentity> readIdentity, string failedCode, string? signInFailedUrl)
    {
        var services = context.HttpContext.RequestServices;
        var token = context.HttpContext.RequestAborted;
        // The handler has already moved the challenge's RedirectUri to ReturnUri.
        var landing = LandingOf(context.Properties, context.ReturnUri, signInFailedUrl);
        try
        {
            var user = await services.GetRequiredService<IExternalAuthService>().ResolveAccount(
                readIdentity(context.Principal!), context.Properties.FindAccountToConnect(), token);

            context.Principal = await services.GetRequiredService<IClaimsPrincipalFactory>().Create(user, scopes: null, token);
            // A provider sign-in survives the browser closing.
            context.Properties!.IsPersistent = true;
        }
        catch (UnauthorizedException refused)
        {
            if (!RedirectWithError(context, landing, refused.Message))
                throw new AuthenticationFailureException(FrameworkFailureMessage, refused);
        }
        catch (Exception failure)
        {
            services.GetService<ILoggerFactory>()?.CreateLogger(typeof(RedirectSignInBinding))
                .LogWarning(failure, "The {Scheme} sign-in could not be bound to an account.", context.Scheme.Name);
            if (!RedirectWithError(context, landing, failedCode))
                throw new AuthenticationFailureException(FrameworkFailureMessage, failure);
        }
    }

    /// <summary>A connect returns to where its challenge asked; a sign-in, or a state that cannot be read, to <paramref name="signInFailedUrl"/>.</summary>
    private static string? LandingOf(AuthenticationProperties? properties, string? returnUri, string? signInFailedUrl)
        => properties.FindAccountToConnect() is null ? signInFailedUrl : returnUri ?? "/";

    private static bool RedirectWithError(HandleRequestContext<RemoteAuthenticationOptions> context, string? landing, string code)
    {
        if (landing is null)
            return false;
        context.Response.Redirect(QueryHelpers.AddQueryString(landing, "error", code));
        context.HandleResponse();
        return true;
    }
}
