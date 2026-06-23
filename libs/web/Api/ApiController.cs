namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Base for API controllers. Actions stay thin: resolve a capability from DI and return its result. Error
/// handling is centralized — the <see cref="Sencilla.Core.SencillaException"/> family surfaces through
/// <see cref="Sencilla.Web.SencillaExceptionHandler"/> as ProblemDetails (400/401/403/…) and any other
/// exception becomes a clean 500 via the host's exception handler. Controllers carry no try/catch and never
/// write exception detail to the response.
/// </summary>
public class ApiController(IServiceProvider provider) : ControllerBase
{
    /// <summary>Resolves an optional service from the request scope (null when not registered).</summary>
    protected TService? R<TService>() => provider.GetService<TService>();

    /// <summary>HTTP 501 — the requested capability isn't registered for this resource.</summary>
    protected IActionResult NotImplemented(string? msg = null)
        => StatusCode((int)HttpStatusCode.NotImplemented, msg);

    /// <summary>
    /// Resolves <typeparamref name="TService"/> and returns its result as 200 OK, or 501 Not Implemented when
    /// the capability isn't registered for this entity (e.g. a read-only entity has no update repository).
    /// Exceptions are intentionally not caught — they propagate to the central exception handler.
    /// </summary>
    protected async Task<IActionResult> FromService<TService, TResult>(Func<TService, Task<TResult>> action)
        where TService : class
    {
        var service = R<TService>();
        return service is null ? NotImplemented() : Ok(await action(service));
    }

    /// <inheritdoc cref="FromService{TService,TResult}(Func{TService,Task{TResult}})"/>
    protected async Task<IActionResult> FromService<TService>(Func<TService, Task> action)
        where TService : class
    {
        var service = R<TService>();
        if (service is null)
            return NotImplemented();

        await action(service);
        return Ok();
    }
}
