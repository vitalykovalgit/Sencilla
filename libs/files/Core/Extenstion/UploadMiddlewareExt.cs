namespace Microsoft.AspNetCore.Builder;

public static class TusResumableUploadMiddlewareExtensions
{
    public static IApplicationBuilder UseTusResumableUpload(this IApplicationBuilder builder, Action<TusResumableUploadOptions> configure)
    {
        var options = new TusResumableUploadOptions { Route = string.Empty};
        configure(options);
        return builder.UseMiddleware<TusResumableUploadMiddleware>(options);
    }

    public static IApplicationBuilder UseTusResumableUpload(this IApplicationBuilder builder, string route)
    {
        var options = new TusResumableUploadOptions() { Route = route };
        return builder.UseMiddleware<TusResumableUploadMiddleware>(options);
    }
}
