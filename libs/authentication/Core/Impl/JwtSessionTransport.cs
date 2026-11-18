namespace Sencilla.Authentication;

/// <summary>
/// <see cref="ISessionTransport"/> of the token mode (<c>UseJwtToken</c>): the token pair is the body.
/// <see cref="SessionLifetime"/> is ignored — the refresh policy decides how long the pair lives.
/// </summary>
public sealed class JwtSessionTransport(ITokenIssuer issuer) : ISessionTransport
{
    public async Task<IResult> SignIn(HttpContext http, ClaimsPrincipal principal, SessionLifetime lifetime, CancellationToken token = default)
        => Results.Ok(await issuer.Issue(principal, token));
}
