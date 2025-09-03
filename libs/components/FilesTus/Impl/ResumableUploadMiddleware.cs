namespace Sencilla.Component.FilesTus;

[DisableInjection]
public class TusResumableUploadMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TusResumableUploadOptions _options;

    public TusResumableUploadMiddleware(RequestDelegate next, TusResumableUploadOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider container)
    {
        if (!context.Request.Path.StartsWithSegments(_options.Route))
        {
            await _next(context);
            return;
        }

        // TODO: Make it allocation free
        var handler = container.GetKeyedService<ITusRequestHandler>(ITusRequestHandler.ServiceKey(context.Request.Method)) ?? throw new NotImplementedException();
        await handler.Handle(context, context.RequestAborted);

        //await TusRequestRouter.Handle(container, new TusContext { HttpContext = context, Configuration = _options });
    }
}
