using System.Net;

namespace Microsoft.AspNetCore.Mvc;

public class ApiController : ControllerBase
{
    protected ILogger? Logger { get; set; }

    protected IResolver Resolver { get; set; }

    public ApiController(IResolver resolver) 
    {
        Resolver = resolver;
        Logger = resolver.Resolve<ILogger>();
    }

    protected TService? R<TService>()
    {
        return Resolver.Resolve<TService>();
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

    #region Handling Requests

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
            var service = R<TService>();
            if (service == null)
                return NotImplemented();

            var data = await handler(service);
            return Ok(data);
        }
        catch (Exception ex)
        {
            Logger?.Error(ex);
            return ExceptionToResponse(ex);
        }
    }

    #endregion
}
