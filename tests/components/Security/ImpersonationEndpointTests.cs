using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;
using Sencilla.Web.MinimalApi;

namespace Sencilla.Component.Security.Tests;

/// <summary>
/// The endpoint's job beyond the cookie: announce the session so the audit log can record it.
///
/// That announcement is the whole reason the routes could move into this component at all — Audit
/// references Security, so Security cannot write the rows itself. If these events stop being published,
/// a read-only impersonation session leaves no trace anywhere and nothing else fails loudly.
/// </summary>
public class ImpersonationEndpointTests
{
    static readonly Guid ActorId = Guid.NewGuid();
    static readonly Guid TargetId = Guid.NewGuid();

    static User Actor() => new() { Id = ActorId, Email = "admin@example.com" };
    static User Target() => new() { Id = TargetId, Email = "customer@example.com" };

    static (ImpersonationEndpoint Endpoint, Mock<IEventDispatcher> Events, SystemVariable Vars, ImpersonationOptions Options,
            Mock<IReadRepository<User, Guid>> Users, Mock<IUserRoleResolver> Roles)
        Harness(User? target = null, HashSet<int>? targetRoles = null)
    {
        var events = new Mock<IEventDispatcher>();
        var vars = new SystemVariable();
        vars.SetCurrentUser(Actor());

        var users = new Mock<IReadRepository<User, Guid>>();
        users.Setup(u => u.GetById(TargetId, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
             .ReturnsAsync(target ?? Target());

        var roles = new Mock<IUserRoleResolver>();
        roles.Setup(r => r.Resolve(It.IsAny<User?>())).Returns(targetRoles ?? [(int)RoleType.User]);

        return (new ImpersonationEndpoint(), events, vars, new ImpersonationOptions(), users, roles);
    }

    static HttpContext Context() => new DefaultHttpContext();

    static IImpersonationContext Impersonating(bool active)
    {
        var ctx = new Mock<IImpersonationContext>();
        ctx.SetupGet(c => c.IsImpersonating).Returns(active);
        ctx.SetupGet(c => c.ImpersonatorId).Returns(active ? ActorId : null);
        return ctx.Object;
    }

    [Fact]
    public async Task Start_PublishesStartedNamingOperatorAndTarget()
    {
        var (endpoint, events, vars, options, users, roles) = Harness();

        var result = await endpoint.StartAsync(
            new ImpersonationEndpoint.StartRequest(TargetId), Context(), options, new ImpersonationCookie(options),
            Impersonating(false), vars, users.Object, roles.Object, events.Object, CancellationToken.None);

        Assert.NotNull(result);
        events.Verify(e => e.PublishAsync(
            It.Is<ImpersonationStartedEvent>(x => x.Operator.Id == ActorId && x.Target.Id == TargetId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Stop_PublishesStoppedWithTheOperatorAsActor()
    {
        var (endpoint, events, _, options, users, _) = Harness();

        // Mid-request the CURRENT user is the impersonated one; the operator has to come back out of the
        // impersonation context, or the stop row would name the customer as the one who stopped.
        var vars = new SystemVariable();
        vars.SetCurrentUser(Target());
        users.Setup(u => u.GetById(ActorId, It.IsAny<CancellationToken>(), It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
             .ReturnsAsync(Actor());

        await endpoint.StopAsync(Context(), new ImpersonationCookie(options), Impersonating(true), vars, users.Object,
            events.Object, CancellationToken.None);

        events.Verify(e => e.PublishAsync(
            It.Is<ImpersonationStoppedEvent>(x => x.Operator.Id == ActorId && x.Target.Id == TargetId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Stop_WithNoLiveSession_AnnouncesNothing()
    {
        var (endpoint, events, vars, options, users, _) = Harness();

        await endpoint.StopAsync(Context(), new ImpersonationCookie(options), Impersonating(false), vars, users.Object,
            events.Object, CancellationToken.None);

        // Clearing an already-clear cookie is a success, not an event — a double-clicked exit must not
        // log a stop that never happened.
        events.Verify(e => e.PublishAsync(It.IsAny<ImpersonationStoppedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Start_RefusedTarget_AnnouncesNothing()
    {
        var (endpoint, events, vars, options, users, roles) = Harness(targetRoles: [(int)RoleType.Root]);

        await endpoint.StartAsync(
            new ImpersonationEndpoint.StartRequest(TargetId), Context(), options, new ImpersonationCookie(options),
            Impersonating(false), vars, users.Object, roles.Object, events.Object, CancellationToken.None);

        events.Verify(e => e.PublishAsync(It.IsAny<ImpersonationStartedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void AddSencillaImpersonation_RegistersTheRoutesWithTheFeature()
    {
        var services = new ServiceCollection();
        services.AddSencillaImpersonation(o => o.Resource = "app.users.impersonate");

        // A middleware nothing can turn on is not a feature: the host calls one method and gets both.
        var endpoints = services.BuildServiceProvider().GetServices<IEndpoint>().ToList();

        Assert.Single(endpoints);
        Assert.IsType<ImpersonationEndpoint>(endpoints[0]);
    }
}
