namespace Sencilla.Authentication;

/// <summary>Maps the authentication services' domain exceptions to HTTP results for the endpoints.</summary>
internal static class AuthResults
{
    public static async Task<IResult> Issue(Func<Task<TokenResponse>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (UnauthorizedException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (BadRequestException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
