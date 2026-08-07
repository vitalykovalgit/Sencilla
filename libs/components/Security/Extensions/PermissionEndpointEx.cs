using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Security;

/// <summary>
/// Declarative permissions for minimal-API endpoints — the non-entity half of the enforcement model.
///
/// Applied as an endpoint filter rather than checked inside the handler so that the check cannot be
/// skipped by an early return, and so the permission a route requires is readable at the route
/// declaration instead of somewhere down the call stack.
/// </summary>
public static class PermissionEndpointEx
{
    /// <summary>
    /// Denies the request with 403 unless the caller holds <paramref name="action"/> on
    /// <paramref name="resource"/>.
    ///
    /// <code>
    /// app.MapPost("api/v1/orders/{id}/pay", Pay)
    ///    .RequirePermission("admin.orders.payment", Action.Update);
    /// </code>
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string resource, Action action) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var checker = context.HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();
            await checker.DemandAsync(resource, action, context.HttpContext.RequestAborted);

            return await next(context);
        });
}
