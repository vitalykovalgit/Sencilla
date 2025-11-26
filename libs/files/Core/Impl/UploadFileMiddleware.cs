namespace Sencilla.Component.Files;

[DisableInjection]
public class UploadFileMiddleware(RequestDelegate next, FileUploadOptions options)
{
    public async Task InvokeAsync(HttpContext context, IServiceProvider container)
    {
        if (!context.Request.Path.StartsWithSegments(options.Route))
        {
            await next(context);
            return;
        }

        // TODO: Make it allocation free
        var handler = container.GetKeyedService<IFileRequestHandler>(IFileRequestHandler.ServiceKey(context.Request.Method)) ?? throw new NotImplementedException();
        await handler.Handle(context, context.RequestAborted);
    }
}
