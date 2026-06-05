namespace Sencilla.Authentication;

/// <summary>
/// Email/password account endpoints — registered by <c>UseAppAccounts</c>, mounted under
/// <see cref="AuthApiOptions.BasePath"/>. Per-request services are injected into the handler
/// methods (see <c>MapSencillaEndpoints</c>); only the singleton options ride on the constructor.
/// </summary>
public sealed class AppAccountsEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/register", Register).WithName("AuthRegister");
        auth.MapPost("/login", Login).WithName("AuthLogin");
    }

    public Task<IResult> Register(RegisterRequest request, ICredentialAuthService credentials, CancellationToken token)
        => AuthResults.Issue(() => credentials.Register(request, token));

    public Task<IResult> Login(LoginRequest request, ICredentialAuthService credentials, CancellationToken token)
        => AuthResults.Issue(() => credentials.Login(request, token));
}
