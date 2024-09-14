namespace Sencilla.Component.Files;

public class TusResumableUploadOptions
{
    public string Route { get; set; }
}

[DisableInjection]
class TusResumableUploadMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TusResumableUploadOptions _options;

    public TusResumableUploadMiddleware(RequestDelegate next, TusResumableUploadOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // add route parameter to this!
        if (!context.Request.Path.StartsWithSegments(_options.Route))
        {
            await _next(context);
            return;
        }

        var handler = HandlerRouter.ResolveHandler(context.Request.Method);
        await handler.Handle(context);
    }
}

public static class TusResumableUploadMiddlewareExtensions
{
    public static IApplicationBuilder UseTusResumableUpload(
        this IApplicationBuilder builder, Action<TusResumableUploadOptions> configure)
    {
        var options = new TusResumableUploadOptions();
        configure(options);
        return builder.UseMiddleware<TusResumableUploadMiddleware>(options);
    }

    public static IApplicationBuilder UseTusResumableUpload(
        this IApplicationBuilder builder, string route)
    {
        var options = new TusResumableUploadOptions() { Route = route };
        return builder.UseMiddleware<TusResumableUploadMiddleware>(options);
    }
}
