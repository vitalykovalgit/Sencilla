namespace Sencilla.Authentication;

/// <summary>Cookie-mode session endpoint (<c>UseCookieSession</c>): <c>POST /logout</c> signs the cookie scheme out.</summary>
public sealed class CookieSessionEndpoint(IOptions<AuthApiOptions> api, IOptions<CookieSessionOptions> session) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/logout", Logout).WithName("AuthLogout");
    }

    public async Task<IResult> Logout(HttpContext http, LogoutRequest request)
    {
        await http.SignOutAsync(session.Value.Scheme);
        return Results.Ok();
    }
}
