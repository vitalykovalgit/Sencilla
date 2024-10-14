namespace Sencilla.Component.Files;

public class TusResumableUploadOptions
{
    public string Route { get; set; }
    public Func<Guid, Task> OnUploadCompleteAsync { get; set; }
}

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
        await TusRequestRouter.Handle(container, new TusContext { HttpContext = context, Configuration = _options });
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
