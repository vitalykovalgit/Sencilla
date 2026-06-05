namespace Sencilla.Authentication;

/// <summary>
/// Social token-exchange endpoint — registered once when any provider (<c>UseGoogle</c>/
/// <c>UseFacebook</c>/<c>UseApple</c>/custom) is configured. Mounted under
/// <see cref="AuthApiOptions.BasePath"/>; the provider key in the body selects the verifier.
/// </summary>
public sealed class ExternalEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/external", External).WithName("AuthExternal");
    }

    public Task<IResult> External(ExternalLoginRequest request, IExternalAuthService external, CancellationToken token)
        => AuthResults.Issue(() => external.Exchange(request.Provider, request.IdToken, token));
}
