namespace Sencilla.Authentication;

/// <summary>The endpoints' HTTP results: the authentication services' domain exceptions mapped to status codes, and the rate-limit refusal.</summary>
internal static class AuthResults
{
    /// <summary>
    /// Run a sign-in: <paramref name="authenticate"/> resolves the user, whose principal the configured
    /// <see cref="ISessionTransport"/> turns into a session and the response body.
    /// </summary>
    public static async Task<IResult> SignIn(HttpContext http, Func<Task<AuthUser>> authenticate, SessionLifetime lifetime, CancellationToken token)
    {
        try
        {
            var user = await authenticate();
            var principal = await http.RequestServices.GetRequiredService<IClaimsPrincipalFactory>().Create(user, scopes: null, token);
            return await http.RequestServices.GetRequiredService<ISessionTransport>().SignIn(http, principal, lifetime, token);
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(ex);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ex);
        }
    }

    public static IResult Unauthorized(SencillaException ex)
        => Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);

    public static IResult BadRequest(SencillaException ex)
        => Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);

    /// <summary>The caller spent a window of <see cref="AuthRateLimitOptions"/>.</summary>
    public static IResult RateLimited()
        => Results.Json(new { error = "rate-limited" }, statusCode: StatusCodes.Status429TooManyRequests);
}
