namespace Sencilla.Web;

/// <summary>
/// Central HTTP mapping for the <see cref="SencillaException"/> family
/// (BadRequest→400, Unauthorized→401, Forbidden→403, anything else→500),
/// written as a ProblemDetails body. Registered by AddSencillaWeb; the host
/// enables it with app.UseSencillaExceptionHandler(). Endpoints and services
/// can throw these exceptions and need no try/catch of their own.
/// Non-Sencilla exceptions are left unhandled so the host default
/// (developer page / generic 500) still applies.
/// </summary>
public class SencillaExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        if (exception is not SencillaException)
            return false;

        var status = exception switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        var detail = string.IsNullOrEmpty(exception.Message) ? null : exception.Message;
        await Results.Problem(detail: detail, statusCode: status).ExecuteAsync(context);
        return true;
    }
}
