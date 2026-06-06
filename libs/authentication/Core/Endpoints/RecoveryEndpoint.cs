namespace Sencilla.Authentication;

/// <summary>
/// Password-recovery endpoints — registered by <c>UseAppAccounts</c>, mounted under
/// <see cref="AuthApiOptions.BasePath"/>. Always returns 200 for forgot-password (enumeration
/// protection); returns 400 with a structured error code on reset-password failure.
/// </summary>
public sealed class RecoveryEndpoint(IOptions<AuthApiOptions> api) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/forgot-password", ForgotPassword).WithName("AuthForgotPassword");
        auth.MapPost("/reset-password", ResetPassword).WithName("AuthResetPassword");
    }

    public async Task<IResult> ForgotPassword(ForgotPasswordRequest request, IAccountRecoveryService recovery, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "email-required" });
        await recovery.RequestPasswordReset(request.Email, token);
        return Results.Ok();
    }

    public async Task<IResult> ResetPassword(ResetPasswordRequest request, IAccountRecoveryService recovery, CancellationToken token)
    {
        try
        {
            await recovery.ResetPassword(request.Token, request.NewPassword, token);
            return Results.Ok();
        }
        catch (BadRequestException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
