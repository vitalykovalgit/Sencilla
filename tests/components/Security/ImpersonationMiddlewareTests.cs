using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

/// <summary>
/// The swap is the whole feature: after this middleware the entire application acts as whoever
/// <c>ISystemVariable.GetCurrentUser()</c> now names, with no further checks anywhere. So every test here
/// asserts on that one value — either it changed, or the request stayed the caller's.
///
/// The rejection cases matter more than the happy path. The cookie is not signed, which means any client
/// can present one; a check that fails open here hands out account takeover.
/// </summary>
public class ImpersonationMiddlewareTests
{
    static readonly Guid ActorId = Guid.NewGuid();
    static readonly Guid TargetId = Guid.NewGuid();

    const string Resource = "admin.users.impersonate";

    static User Actor() => new() { Id = ActorId, Email = "admin@example.com" };
    static User Target() => new() { Id = TargetId, Email = "customer@example.com" };

    /// <summary>A cookie value in the wire format: id plus an absolute deadline.</summary>
    static string Cookie(Guid targetId, int minutesFromNow = 30)
        => $"{targetId}|{DateTimeOffset.UtcNow.AddMinutes(minutesFromNow).ToUnixTimeSeconds()}";

    /// <summary>Runs the middleware over a request carrying <paramref name="cookie"/> and returns the effective user.</summary>
    static async Task<(User? Effective, bool Impersonating, HttpContext Context)> RunAsync(
        string? cookie,
        User? actor = null,
        User? target = null,
        bool granted = true,
        HashSet<int>? targetRoles = null,
        bool enabled = true)
    {
        var options = new ImpersonationOptions { Enabled = enabled, Resource = Resource };

        var sysVars = new SystemVariable();
        if (actor != null)
            sysVars.SetCurrentUser(actor);

        var permissions = new Mock<IPermissionChecker>();
        permissions.Setup(p => p.HasAsync(Resource, It.IsAny<Action>(), It.IsAny<CancellationToken>())).ReturnsAsync(granted);

        var roles = new Mock<IUserRoleResolver>();
        roles.Setup(r => r.Resolve(It.IsAny<User?>())).Returns(targetRoles ?? [(int)RoleType.User]);

        var users = new Mock<IReadRepository<User, Guid>>();
        users.Setup(u => u.GetById(TargetId, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
             .ReturnsAsync(target);

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<ImpersonationCookie>();
        services.AddSingleton<ISystemVariable>(sysVars);
        services.AddSingleton<IImpersonationContext, ImpersonationContext>();
        services.AddSingleton(permissions.Object);
        services.AddSingleton(roles.Object);
        services.AddSingleton(users.Object);

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        if (cookie != null)
            context.Request.Headers.Cookie = $"impersonate={cookie}";

        var called = false;
        await new ImpersonationMiddleware(_ => { called = true; return Task.CompletedTask; }).Invoke(context);

        Assert.True(called, "the pipeline must always continue — a bad cookie degrades to an ordinary session, never a 403");

        var impersonation = context.RequestServices.GetRequiredService<IImpersonationContext>();
        return (sysVars.GetCurrentUser(), impersonation.IsImpersonating, context);
    }

    static bool ClearsCookie(HttpContext context) =>
        context.Response.Headers.SetCookie.Any(h => h!.StartsWith("impersonate=", StringComparison.Ordinal) && h.Contains("expires=Thu, 01 Jan 1970"));

    // ── the swap ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidCookie_SwapsTheCurrentUser()
    {
        var (effective, impersonating, _) = await RunAsync(Cookie(TargetId), Actor(), Target());

        Assert.Equal(TargetId, effective!.Id);
        Assert.True(impersonating);
    }

    [Fact]
    public async Task NoCookie_LeavesTheCallerAlone()
    {
        var (effective, impersonating, _) = await RunAsync(cookie: null, Actor(), Target());

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
    }

    // ── rejections (each one is a takeover if it fails open) ─────────────────

    [Fact]
    public async Task WithoutTheGrant_DoesNotSwap()
    {
        // The matrix row is the entire access control for the feature.
        var (effective, impersonating, context) = await RunAsync(Cookie(TargetId), Actor(), Target(), granted: false);

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
        Assert.True(ClearsCookie(context));
    }

    [Fact]
    public async Task RootTarget_DoesNotSwap()
    {
        // Root is break-glass everywhere else in this component; it is never a target, grant or no grant.
        var (effective, impersonating, context) = await RunAsync(
            Cookie(TargetId), Actor(), Target(), targetRoles: [(int)RoleType.User, (int)RoleType.Root]);

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
        Assert.True(ClearsCookie(context));
    }

    [Fact]
    public async Task AnonymousCaller_DoesNotSwap()
    {
        // A cookie presented without a session must not become a session.
        var (effective, impersonating, _) = await RunAsync(Cookie(TargetId), actor: null, Target());

        Assert.Null(effective);
        Assert.False(impersonating);
    }

    [Fact]
    public async Task UnknownTarget_DoesNotSwap()
    {
        var (effective, impersonating, context) = await RunAsync(Cookie(TargetId), Actor(), target: null);

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
        Assert.True(ClearsCookie(context));
    }

    [Fact]
    public async Task DeletedTarget_DoesNotSwap()
    {
        var deleted = Target();
        deleted.DeletedDate = DateTime.UtcNow;

        var (effective, impersonating, _) = await RunAsync(Cookie(TargetId), Actor(), deleted);

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
    }

    [Fact]
    public async Task GarbageCookie_DoesNotSwap()
    {
        var (effective, impersonating, _) = await RunAsync("not-a-guid|nope", Actor(), Target());

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
    }

    [Fact]
    public async Task ExpiredCookie_DoesNotSwap()
    {
        // The browser's own Expires is advisory — a client that ignores it must not get an unbounded
        // session, so the deadline travels in the value and is checked here.
        var (effective, impersonating, context) = await RunAsync(Cookie(TargetId, minutesFromNow: -1), Actor(), Target());

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
        Assert.True(ClearsCookie(context));
    }

    [Fact]
    public async Task NoCookie_WritesNoSetCookieHeader()
    {
        // Clearing unconditionally would put a delete header on every response in the application.
        var (_, _, context) = await RunAsync(cookie: null, Actor(), Target());

        // StringValues, not a collection — Count is 0 when the header was never written.
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Disabled_IsAPassThrough()
    {
        // The kill switch has to win before any lookup — that is what makes it usable in an incident.
        var (effective, impersonating, _) = await RunAsync(Cookie(TargetId), Actor(), Target(), enabled: false);

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
    }

    [Fact]
    public async Task SelfCookie_IsANoOp()
    {
        var self = Actor();
        var (effective, impersonating, _) = await RunAsync(Cookie(ActorId), self, self);

        Assert.Equal(ActorId, effective!.Id);
        Assert.False(impersonating);
    }
}
