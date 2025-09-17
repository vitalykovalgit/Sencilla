namespace Sencilla.Component.Files;

[DisableInjection]
public class TusResumableUploadMiddleware(RequestDelegate next, TusResumableUploadOptions options)
{
    public async Task InvokeAsync(HttpContext context, IServiceProvider container)
    {
        if (!context.Request.Path.StartsWithSegments(options.Route))
        {
            await next(context);
            return;
        }

        // TODO: Make it allocation free
        var handler = container.GetKeyedService<ITusRequestHandler>(ITusRequestHandler.ServiceKey(context.Request.Method)) ?? throw new NotImplementedException();
        await handler.Handle(context, context.RequestAborted);

        //await TusRequestRouter.Handle(container, new TusContext { HttpContext = context, Configuration = _options });
    }
}
