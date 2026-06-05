namespace Sencilla.Authentication;

/// <summary>Projection of the validated principal returned by the optional <c>/me</c> endpoint.</summary>
public sealed record MeResponse(string? Id, string? Email, bool EmailVerified, string? Name, string? Picture, string[] Roles);

/// <summary>
/// Protected current-user endpoint — opt-in via <c>UseApi(o =&gt; o.MapMe())</c>, and only when a
/// bearer scheme is configured (<c>AcceptBearer</c>). Projects the validated claims principal.
/// </summary>
public sealed class MeEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet(api.Value.MePath, GetMe).RequireAuthorization().WithName("AuthMe");

    public IResult GetMe(ClaimsPrincipal user) => Results.Ok(Project(user));

    private static MeResponse Project(ClaimsPrincipal user)
    {
        var id = user.FindFirst(AuthClaims.Subject)?.Value
              ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(AuthClaims.Email)?.Value
                 ?? user.FindFirst(ClaimTypes.Email)?.Value;
        var emailVerified = string.Equals(user.FindFirst(AuthClaims.EmailVerified)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var name = user.FindFirst(AuthClaims.Name)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value;
        var picture = user.FindFirst(AuthClaims.Picture)?.Value;
        var roles = user.FindAll(AuthClaims.Roles).Select(c => c.Value)
            .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToArray();

        return new MeResponse(id, email, emailVerified, name, picture, roles);
    }
}
