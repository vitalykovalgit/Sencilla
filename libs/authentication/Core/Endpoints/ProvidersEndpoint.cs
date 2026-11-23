namespace Sencilla.Authentication;

/// <summary>
/// Which redirect sign-ins the app offers, so a client shows a button only for those: <c>GET {BasePath}/providers</c>,
/// anonymous, answers <c>[{ "provider": "google", "scheme": "Google" }, …]</c> in registration order (<c>[]</c> when
/// none). A host challenges the <c>scheme</c> to start one.
/// </summary>
public sealed class ProvidersEndpoint(IOptions<AuthApiOptions> api, IEnumerable<RedirectSignInProvider> providers) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet(api.Value.BasePath + "/providers", GetProviders).AllowAnonymous().WithName("AuthProviders");

    public IResult GetProviders() => Results.Ok(providers);
}
