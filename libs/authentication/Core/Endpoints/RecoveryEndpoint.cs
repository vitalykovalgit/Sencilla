namespace Sencilla.Authentication;

/// <summary>
/// Password-recovery and address-verification endpoints — registered by <c>UseAppAccounts</c>,
/// mounted under <see cref="AuthApiOptions.BasePath"/>. Always returns 200 for forgot-password
/// (enumeration protection); returns 400 with a structured error code when a token is rejected.
/// A successful reset signs the user in through the configured <see cref="ISessionTransport"/>.
/// </summary>
public sealed class RecoveryEndpoint(IOptions<AuthApiOptions> api, AuthRateLimiter limiter) : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(api.Value.BasePath).AllowAnonymous();

        auth.MapPost("/forgot-password", ForgotPassword).WithName("AuthForgotPassword");
        auth.MapPost("/reset-password", ResetPassword).WithName("AuthResetPassword");
        auth.MapPost("/verify-email", VerifyEmail).WithName("AuthVerifyEmail");

        app.MapPost(api.Value.BasePath + "/resend-verification", ResendVerification)
           .RequireAuthorization()
           .WithName("AuthResendVerification");
    }

    public async Task<IResult> ForgotPassword(HttpContext http, ForgotPasswordRequest request, IAccountRecoveryService recovery, CancellationToken token)
    {
        if (!limiter.AllowsMail(http, request.Email))
            return AuthResults.RateLimited();
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "email-required" });
        await recovery.RequestPasswordReset(request.Email, token);
        return Results.Ok();
    }

    public async Task<IResult> ResetPassword(HttpContext http, ResetPasswordRequest request, IAccountRecoveryService recovery, CancellationToken token)
    {
        if (!limiter.AllowsTokenUse(http))
            return AuthResults.RateLimited();
        return await AuthResults.SignIn(http, () => recovery.ResetPassword(request.Token, request.NewPassword, token), new SessionLifetime(), token);
    }

    public async Task<IResult> VerifyEmail(HttpContext http, VerifyEmailRequest request, IAccountRecoveryService recovery, CancellationToken token)
    {
        if (!limiter.AllowsTokenUse(http))
            return AuthResults.RateLimited();
        try
        {
            var user = await recovery.ConfirmEmail(request.Token, token);
            return Results.Ok(new { verified = true, email = user.Email });
        }
        catch (BadRequestException ex)
        {
            return AuthResults.BadRequest(ex);
        }
    }

    public async Task<IResult> ResendVerification(HttpContext http, ResendVerificationRequest request, IAccountRecoveryService recovery, CancellationToken token)
    {
        if (http.User.GetUserId() is not { } userId)
            return Results.Unauthorized();
        if (!limiter.AllowsMail(http, userId.ToString()))
            return AuthResults.RateLimited();
        await recovery.RequestEmailVerification(userId, token);
        return Results.Ok();
    }
}
