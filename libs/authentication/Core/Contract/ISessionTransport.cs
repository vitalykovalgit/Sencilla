namespace Sencilla.Authentication;

/// <summary>
/// How a signed-in principal's session reaches the client: a token pair in the body
/// (<see cref="JwtSessionTransport"/>, <c>UseJwtToken</c>) or the host's cookie
/// (<see cref="CookieSessionTransport"/>, <c>UseCookieSession</c>). Every sign-in endpoint goes through it.
/// </summary>
public interface ISessionTransport
{
    /// <summary>Start a session for the principal on this request and return the sign-in response.</summary>
    Task<IResult> SignIn(HttpContext http, ClaimsPrincipal principal, SessionLifetime lifetime, CancellationToken token = default);
}
