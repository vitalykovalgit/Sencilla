namespace Sencilla.Web.MinimalApi
{
    /// <summary>
    /// Endpoint filter that enables synchronous IO for Minimal API endpoints.
    /// Use this filter when you need to perform synchronous IO operations like creating ZIP archives.
    /// </summary>
    public class AllowSynchronousIOFilter: IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var syncIOFeature = context.HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
                syncIOFeature.AllowSynchronousIO = true;

            return await next(context);
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding common endpoint filters to Minimal API endpoints.
    /// </summary>
    public static class AllowSynchronousIOFilterExtensions
    {
        /// <summary>
        /// Adds a filter that enables synchronous IO for the endpoint.
        /// Use this when you need to perform synchronous IO operations like creating ZIP archives.
        /// </summary>
        public static RouteHandlerBuilder AllowSynchronousIO(this RouteHandlerBuilder builder)
        {
            return builder.AddEndpointFilter<AllowSynchronousIOFilter>();
        }
    }
}