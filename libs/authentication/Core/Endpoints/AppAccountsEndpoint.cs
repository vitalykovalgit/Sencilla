namespace Sencilla.Authentication;

/// <summary>
/// Email/password account endpoints — registered by <c>UseAppAccounts</c>, mounted under
/// <see cref="AuthApiOptions.BasePath"/>. Per-request services are injected into the handler
/// methods (see <c>MapSencillaEndpoints</c>); only singletons (the options, the rate limiter) ride on the constructor.
/// </summary>
public sealed class AppAccountsEndpoint(IOptions<AuthApiOptions> api, AuthRateLimiter limiter, IOptions<PasswordPolicyOptions> password) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/register", Register).WithName("AuthRegister");
        auth.MapPost("/login", Login).WithName("AuthLogin");
        auth.MapGet("/password-policy", GetPasswordPolicy).WithName("AuthPasswordPolicy");

        app.MapPost(api.Value.BasePath + "/change-password", ChangePassword)
           .RequireAuthorization()
           .WithName("AuthChangePassword");
    }

    // A freshly registered user stays signed in on this device.
    public async Task<IResult> Register(HttpContext http, RegisterRequest request, ICredentialAuthService credentials, CancellationToken token)
    {
        if (!limiter.AllowsRegistration(http))
            return AuthResults.RateLimited();
        return await AuthResults.SignIn(http, () => credentials.Register(request, token), new SessionLifetime(Persistent: true), token);
    }

    public async Task<IResult> Login(HttpContext http, LoginRequest request, ICredentialAuthService credentials, CancellationToken token)
    {
        if (!limiter.AllowsLogin(http, request.Login))
            return AuthResults.RateLimited();
        return await AuthResults.SignIn(http, () => credentials.Login(request, token), new SessionLifetime(request.RememberMe), token);
    }

    /// <summary>The rules a new password must meet, so a form can check it before sending — never the pepper or the Argon2 cost.</summary>
    public IResult GetPasswordPolicy()
    {
        var policy = password.Value;
        return Results.Ok(new { policy.MinLength, policy.MaxLength, policy.RequireDigit, policy.RequireMixedCase });
    }

    public async Task<IResult> ChangePassword(
        HttpContext http, ChangePasswordRequest request, ICredentialAuthService credentials, IUserStore users, CancellationToken token)
    {
        if (!limiter.AllowsTokenUse(http))
            return AuthResults.RateLimited();
        if (http.User.GetUserId() is not { } userId)
            return Results.Unauthorized();

        // The change stamps CredentialsChangedAt, which retires every session issued before it — including
        // the one making this call. Re-issue it so the owner stays signed in; everyone else is out.
        return await AuthResults.SignIn(http, async () =>
        {
            await credentials.ChangePassword(userId, request.CurrentPassword, request.NewPassword, token);
            return await users.FindById(userId, token) ?? throw new UnauthorizedException("user-not-found");
        }, new SessionLifetime(Persistent: true), token);
    }
}
