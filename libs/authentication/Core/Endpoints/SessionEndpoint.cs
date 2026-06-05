namespace Sencilla.Authentication;

/// <summary>
/// Session-lifecycle endpoints owned by the token issuer (<c>UseJwtToken</c>): rotating-refresh
/// exchange and logout (revokes the refresh-token family). Mounted under
/// <see cref="AuthApiOptions.BasePath"/>.
/// </summary>
public sealed class SessionEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/refresh", Refresh).WithName("AuthRefresh");
        auth.MapPost("/logout", Logout).WithName("AuthLogout");
    }

    public async Task<IResult> Refresh(
        RefreshRequest request,
        RefreshTokenService refresh,
        IUserStore store,
        IClaimsPrincipalFactory principals,
        ITokenIssuer issuer,
        CancellationToken token)
    {
        var result = await refresh.Redeem(request.RefreshToken, token);
        if (result.Token is null || result.UserId is null)
            return Results.Unauthorized();

        var user = await store.FindById(result.UserId.Value, token);
        if (user is null)
            return Results.Unauthorized();

        var principal = await principals.Create(user, scopes: null, token);
        var response = await issuer.IssueAccessToken(principal, token);

        return Results.Ok(response with { RefreshToken = result.Token });
    }

    public async Task<IResult> Logout(RefreshRequest request, IRefreshTokenStore store, CancellationToken token)
    {
        var existing = await store.Find(request.RefreshToken, token);
        if (existing is not null)
            await store.RevokeFamily(existing.FamilyId, token);
        return Results.Ok();
    }
}
