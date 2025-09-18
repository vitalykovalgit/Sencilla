namespace Microsoft.AspNetCore.Builder;

public static class SignalRApplicationExtensions
{
    /// <summary>
    /// Maps the SignalR messaging hub to the application pipeline.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="path">The path to map the hub to (default: /messaging)</param>
    /// <returns></returns>
    public static IApplicationBuilder UseSignalRMessaging(this IApplicationBuilder app, string path = "/messaging")
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<MessagingHub>(path);
        });
        
        return app;
    }
}