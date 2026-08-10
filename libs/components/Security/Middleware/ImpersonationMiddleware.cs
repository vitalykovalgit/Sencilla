using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Security;

/// <summary>
/// Re-points the request's current user at the impersonated account. Chain AFTER <c>UseSencillaUser()</c>
/// (which resolves the real user) and BEFORE <c>UseSencillaAudit()</c> and the endpoints.
///
/// Everything downstream — row-scoping constraints, role resolution, permission checks,
/// CreatedBy/UpdatedBy stamps — reads <c>ISystemVariable.GetCurrentUser()</c>, so overwriting that one
/// value impersonates the whole application. Nothing else needs to know.
///
/// Validation runs on EVERY request, not once at the endpoint, for two independent reasons: the cookie
/// is not signed (any client can present one), and a grant revoked mid-session must take effect within
/// the role cache window rather than at the end of the 30-minute lifetime. A cookie that fails any check
/// is cleared and the request proceeds as the real user — fail-closed, never a 403, so a stale cookie
/// degrades to an ordinary session instead of locking the admin out of the app.
/// </summary>
[DisableInjection]
public class ImpersonationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var services = context.RequestServices;

        var options = services.GetService<ImpersonationOptions>();
        var cookie = services.GetService<ImpersonationCookie>();
        if (options is not { Enabled: true } || cookie is null)
        {
            await next(context);
            return;
        }

        // Nothing to do on the overwhelming majority of requests — and no Set-Cookie either: clearing
        // unconditionally would put a delete header on every response in the application.
        if (!context.Request.Cookies.ContainsKey(options.CookieName))
        {
            await next(context);
            return;
        }

        var live = cookie.Read(context);

        // Malformed or past its deadline, or it failed validation. Clearing here is what makes the
        // lifetime self-healing: the next request is an ordinary session again, with no client help.
        if (live is null || !await TrySwapAsync(context, services, live.Value.TargetId, options))
            cookie.Clear(context);

        await next(context);
    }

    static async Task<bool> TrySwapAsync(HttpContext context, IServiceProvider services, Guid targetId, ImpersonationOptions options)
    {
        var sysVars = services.GetRequiredService<ISystemVariable>();

        // The real caller, as UserRegistrationMiddleware resolved them. Anonymous cannot impersonate.
        var actor = sysVars.GetCurrentUser();
        if (actor is null || actor.Id == Guid.Empty)
            return false;

        // A self-referential cookie is a no-op, not an error — leave the session alone.
        if (actor.Id == targetId)
            return false;

        // The whole access control for the feature (IM10/IM18): one matrix grant, re-checked per request.
        var permissions = services.GetRequiredService<IPermissionChecker>();
        if (!await permissions.HasAsync(options.Resource, options.Action, context.RequestAborted))
            return false;

        var users = services.GetRequiredService<IReadRepository<User, Guid>>();
        var roles = services.GetRequiredService<IUserRoleResolver>();

        User? target;
        // Root access for the lookup itself: the admin's own matrix constraints would row-scope this
        // read and silently 404 exactly the users impersonation exists to reach.
        using (Access.Root())
            target = await users.GetById(targetId, context.RequestAborted);

        if (target is null || target.DeletedDate is not null)
            return false;

        // Root is never a target, independent of any grant — the same break-glass role the constraint
        // and permission paths hard-code ahead of every matrix lookup.
        if (roles.Resolve(target).Contains((int)RoleType.Root))
            return false;

        services.GetRequiredService<IImpersonationContext>().Begin(actor, target);
        sysVars.SetCurrentUser(target);

        return true;
    }
}
