namespace Sencilla.Authentication;

/// <summary>
/// <see cref="ISessionTransport"/> of the cookie session mode (<c>UseCookieSession</c>): signs the
/// principal into the host's cookie scheme as it is, instead of minting tokens, and returns the user projection.
/// </summary>
public sealed class CookieSessionTransport(IOptions<CookieSessionOptions> options) : ISessionTransport
{
    public async Task<IResult> SignIn(HttpContext http, ClaimsPrincipal principal, SessionLifetime lifetime, CancellationToken token = default)
    {
        await http.SignInAsync(options.Value.Scheme, principal, new AuthenticationProperties { IsPersistent = lifetime.Persistent });
        http.User = principal;

        return Results.Ok(SessionUser.Project(principal));
    }
}
