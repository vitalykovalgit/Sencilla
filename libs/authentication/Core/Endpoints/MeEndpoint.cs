namespace Sencilla.Authentication;

/// <summary>Projection of the validated principal returned by the optional <c>/me</c> endpoint and by the cookie-mode sign-in endpoints.</summary>
public sealed record MeResponse(string? Id, string? Email, bool EmailVerified, string? Name, string? Picture, string[] Roles);

/// <summary>
/// Protected current-user endpoint — opt-in via <c>UseApi(o =&gt; o.MeEnabled = true)</c>, when a bearer scheme
/// (<c>AcceptBearer</c>) or a cookie session (<c>UseCookieSession</c>) is configured.
/// </summary>
public sealed class MeEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet(api.Value.MePath, GetMe).RequireAuthorization().WithName("AuthMe");

    public IResult GetMe(ClaimsPrincipal user) => Results.Ok(SessionUser.Project(user));
}
