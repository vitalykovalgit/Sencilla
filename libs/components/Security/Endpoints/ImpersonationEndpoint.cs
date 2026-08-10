using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sencilla.Web.MinimalApi;

namespace Sencilla.Component.Security;

/// <summary>
/// Start, inspect and stop an impersonation session. The swap itself is
/// <see cref="ImpersonationMiddleware"/>'s job — these routes only manage the cookie that drives it, and
/// announce that a human decided to.
///
/// Registered by <c>AddSencillaImpersonation()</c> alongside the cookie and the context, so a host gets
/// the whole feature from one call and cannot end up with a middleware that nothing can turn on.
///
/// The resource id and action come from <see cref="ImpersonationOptions"/> rather than a constant here:
/// the matrix namespace belongs to the host, and the middleware re-checks the same pair on every
/// subsequent request — this check alone would only cover the moment the session began.
/// </summary>
public class ImpersonationEndpoint : IEndpoint
{
    public record StartRequest(Guid UserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<ImpersonationOptions>();

        app.MapPost("api/v1/impersonation", StartAsync)
           .WithName("StartImpersonation")
           .RequireAuthorization()
           // Declarative so the gate is visible at the route and cannot be skipped by an early return.
           .RequirePermission(options.Resource, options.Action);

        app.MapDelete("api/v1/impersonation", StopAsync)
           .WithName("StopImpersonation")
           .RequireAuthorization();

        // Anonymous is allowed and simply gets an empty collection. The client asks on every boot,
        // including a storefront's anonymous visitors, and a 401 there would be noise the client has to
        // special-case — an anonymous caller is by definition not impersonating anyone.
        app.MapGet("api/v1/impersonation", StatusAsync)
           .WithName("GetImpersonation");

        // Browser-navigable escape route (not under /api): the sessions worth impersonating for include
        // the ones where the SPA does not boot, and then the in-app exit button is inside the failure.
        app.MapGet("impersonation/exit", ExitAsync)
           .WithName("ExitImpersonation")
           .AllowAnonymous();
    }

    public async Task<IResult> StartAsync(
        StartRequest request,
        HttpContext httpContext,
        ImpersonationOptions options,
        ImpersonationCookie cookie,
        IImpersonationContext impersonation,
        ISystemVariable systemVars,
        IReadRepository<User, Guid> users,
        IUserRoleResolver roles,
        IEventDispatcher events,
        CancellationToken token)
    {
        if (!options.Enabled)
            return Results.NotFound();

        // No nesting. Checked before anything else because everything after it would be evaluated as the
        // CURRENT (already impersonated) identity rather than the operator's.
        if (impersonation.IsImpersonating)
            return Results.Conflict(new { error = "impersonation-already-active" });

        var actor = systemVars.GetCurrentUser();
        if (actor is null || actor.Id == Guid.Empty)
            return Results.Unauthorized();

        if (request.UserId == Guid.Empty || request.UserId == actor.Id)
            return Results.BadRequest(new { error = "impersonation-invalid-target" });

        User? target;
        // The operator's own matrix constraints would row-scope this read and 404 exactly the users the
        // feature exists to reach.
        using (Access.Root())
            target = await users.GetById(request.UserId, token);

        if (target is null || target.DeletedDate is not null)
            return Results.NotFound(new { error = "impersonation-unknown-target" });

        // Root is never a target, grant or no grant — the same break-glass role the constraint and
        // permission paths hard-code ahead of every matrix lookup. Enforced here AND in the middleware:
        // this one produces a readable error, that one is the guarantee.
        if (roles.Resolve(target).Contains((int)RoleType.Root))
            return Results.Json(new { error = "impersonation-forbidden-target" }, statusCode: StatusCodes.Status403Forbidden);

        var expiresAt = cookie.Set(httpContext, target.Id);

        await events.PublishAsync(new ImpersonationStartedEvent { Operator = actor, Target = target }, token);

        return Results.Ok(Status(target, actor.Id, actor.Email, expiresAt));
    }

    public async Task<IResult> StopAsync(
        HttpContext httpContext,
        ImpersonationCookie cookie,
        IImpersonationContext impersonation,
        ISystemVariable systemVars,
        IReadRepository<User, Guid> users,
        IEventDispatcher events,
        CancellationToken token)
    {
        cookie.Clear(httpContext);

        // Idempotent: clearing an already-clear session is a success, so a double click or a stale tab
        // never produces an error the operator has to interpret — and never announces a stop.
        if (!impersonation.IsImpersonating)
            return Results.NoContent();

        // Mid-request the current user IS the impersonated one — the operator has to be read back out of
        // the impersonation context, or the stop would name the customer as the one who stopped.
        var target = systemVars.GetCurrentUser();
        User? actor;
        using (Access.Root())
            actor = await users.GetById(impersonation.ImpersonatorId!.Value, token);

        if (actor is not null && target is not null)
            await events.PublishAsync(new ImpersonationStoppedEvent { Operator = actor, Target = target }, token);

        return Results.NoContent();
    }

    /// <summary>
    /// The live session, and until when. The client's data source, and the deadline it counts down
    /// against — a tab left open past the lifetime must reload itself rather than silently start showing
    /// the operator's own data again.
    ///
    /// A COLLECTION with at most one member, not a single object with an `impersonating` flag: that is
    /// the shape every other read has, so a client reads it with its ordinary entity loader instead of a
    /// bespoke fetch, and "no session" is an empty list rather than a sentinel.
    /// </summary>
    public IResult StatusAsync(
        HttpContext httpContext,
        ImpersonationOptions options,
        ImpersonationCookie cookie,
        IImpersonationContext impersonation,
        ISystemVariable systemVars)
    {
        var target = systemVars.GetCurrentUser();

        if (!options.Enabled || !impersonation.IsImpersonating || target is null)
            return Results.Ok(Array.Empty<object>());

        // Read back rather than recomputed: the deadline is absolute and set when the session started,
        // so recomputing it from the lifetime here would silently extend it on every poll.
        var expiresAt = cookie.Read(httpContext)?.ExpiresAt ?? DateTimeOffset.UtcNow;

        return Results.Ok(new[] { Status(target, impersonation.ImpersonatorId!.Value, impersonation.ImpersonatorEmail, expiresAt) });
    }

    /// <summary>Clears the cookie and bounces back to the app. Safe to hit when nothing is active.</summary>
    public IResult ExitAsync(HttpContext httpContext, ImpersonationOptions options, ImpersonationCookie cookie)
    {
        cookie.Clear(httpContext);

        return Results.Redirect(options.ExitRedirectUrl);
    }

    static object Status(User target, Guid operatorId, string? operatorEmail, DateTimeOffset expiresAt) => new
    {
        userId = target.Id,
        email = target.Email,
        name = DisplayName(target),
        operatorId,
        operatorEmail,
        expiresAt,
    };

    static string DisplayName(User user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();

        return name.Length > 0 ? name : user.Email ?? user.Id.ToString();
    }
}
