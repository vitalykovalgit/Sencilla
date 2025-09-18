namespace Microsoft.AspNetCore.Mvc;

public class ApiController(IServiceProvider provider) : ControllerBase
{
    protected TService? R<TService>()
    {
        return provider.GetService<TService>();
    }

    #region Custom Statuses

    protected IActionResult NotImplemented(string? msg = null)
    {
        return base.StatusCode((int)HttpStatusCode.NotImplemented, msg);
    }

    protected IActionResult ResourceGone(string? msg = null)
    {
        return StatusCode((int)HttpStatusCode.Gone, msg);
    }

    protected IActionResult Forbidden(string? msg = null)
    {
        return StatusCode((int)HttpStatusCode.Forbidden, msg);
    }

    protected IActionResult Unauthorized(string? msg = null)
    {
        return StatusCode((int)HttpStatusCode.Unauthorized, msg);
    }


    protected IActionResult InternalServerError(Exception? ex = null)
    {
        return StatusCode((int)HttpStatusCode.InternalServerError, $"{ex?.Message}\r\n{ex?.StackTrace}");
    }

    #endregion


    protected IActionResult ExceptionToResponse(Exception ex)
    {
        //if (ex is EntityNotExistException)
        //    return NotFound(ex.Message);

        if (ex is UnauthorizedException)
            return Unauthorized(ex.Message);

        if (ex is ForbiddenException)
            return Forbidden(ex.Message);

        return InternalServerError(ex);
    }

    protected async Task<IActionResult> AjaxAction<TService, TResult>(Func<TService, Task<TResult>> handler)
    {
        try
        {
            var service = provider.GetService<TService>();
            if (service == null)
                return NotImplemented();

            var data = await handler(service);
            return Ok(data);
        }
        catch (Exception ex)
        {
            provider.GetService<ILogger>()?.LogError(ex, "AjaxAction error");
            return ExceptionToResponse(ex);
        }
    }

}
