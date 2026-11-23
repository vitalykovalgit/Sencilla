namespace Sencilla.Authentication;

/// <summary>
/// The signed-in account's own ways in, mounted under <see cref="AuthApiOptions.BasePath"/>; every route needs a
/// session. A provider is connected by the ordinary redirect sign-in with a connect intent
/// (<see cref="AuthenticationPropertiesEx.ConnectToAccountOf"/>), not here.
/// <list type="bullet">
/// <item><c>GET /logins</c> → <c>{ "providers": ["google", …], "hasPassword": bool }</c>.</item>
/// <item><c>DELETE /logins/{provider}</c> → 200, also when that provider is not linked; 400
/// <c>{ "error": "last-sign-in-method" }</c> when it is the account's only way in.</item>
/// <item><c>GET /phone-on-another-account</c> → <c>{ "exists": bool }</c>: the account's phone is confirmed and
/// another account has the same number typed — the hint to sign in there and connect the provider instead.</item>
/// </list>
/// </summary>
public sealed class LoginsEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var account = app.MapGroup(api.Value.BasePath).RequireAuthorization();

        account.MapGet("/logins", GetLogins).WithName("AuthLogins");
        account.MapDelete("/logins/{provider}", DisconnectProvider).WithName("AuthDisconnectProvider");
        account.MapGet("/phone-on-another-account", IsPhoneOnAnotherAccount).WithName("AuthPhoneOnAnotherAccount");
    }

    public async Task<IResult> GetLogins(HttpContext http, IUserStore users, CancellationToken token)
    {
        if (http.User.GetUserId() is not { } userId)
            return Results.Unauthorized();
        return Results.Ok(new
        {
            providers = await users.GetLinkedProviders(userId, token),
            hasPassword = await users.HasPasswordCredential(userId, token),
        });
    }

    public async Task<IResult> DisconnectProvider(HttpContext http, string provider, IAccountLinkService links, CancellationToken token)
    {
        if (http.User.GetUserId() is not { } userId)
            return Results.Unauthorized();
        try
        {
            await links.UnlinkProvider(userId, provider, token);
            return Results.Ok();
        }
        catch (BadRequestException ex)
        {
            return AuthResults.BadRequest(ex);
        }
    }

    public async Task<IResult> IsPhoneOnAnotherAccount(HttpContext http, IUserStore users, CancellationToken token)
    {
        if (http.User.GetUserId() is not { } userId)
            return Results.Unauthorized();
        return Results.Ok(new { exists = await users.IsConfirmedPhoneOnAnotherAccount(userId, token) });
    }
}
