using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

/// <summary>
/// The endpoint half of enforcement. The invariants that matter here are the ones that turn a missing row
/// into an open door: fail-closed on no match, no grant from a row whose role the caller does not hold,
/// and no grant from a constraint this API cannot evaluate.
/// </summary>
public class PermissionCheckerTests
{
    private const string Resource = "admin.orders.payment";

    private static PermissionChecker Checker(User? user, HashSet<int> held, params Matrix[] rows)
    {
        var sysVars = new Mock<ISystemVariable>();
        sysVars.Setup(s => s.Get<User>(It.IsAny<string>())).Returns(user!);

        var roles = new Mock<IUserRoleResolver>();
        roles.Setup(r => r.Resolve(It.IsAny<User?>())).Returns(held);
        roles.Setup(r => r.ResolveExpanded(It.IsAny<User?>())).Returns(held);

        var provider = new Mock<ISecurityProvider>();
        provider.Setup(p => p.Permissions(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<Action?>()))
                .ReturnsAsync((CancellationToken _, string resource, Action? action) => rows
                    .Where(r => string.Equals(r.Resource, resource, StringComparison.OrdinalIgnoreCase))
                    .Where(r => action == null || (r.Action & (int)action.Value) == (int)action.Value));
        provider.Setup(p => p.GetAllPermissions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows.AsQueryable());

        return new PermissionChecker(sysVars.Object, provider.Object, roles.Object, Mock.Of<IServiceProvider>());
    }

    private static Matrix Row(int roleId, Action action, string? constraint = null, string resource = Resource) =>
        new() { RoleId = roleId, Resource = resource, Action = (int)action, Constraint = constraint };

    private static readonly User Manager = new() { Email = "m@example.com", Id = Guid.NewGuid() };

    [Fact]
    public async Task NoRow_Denies()
    {
        // Fail closed: the whole point of the model is that an unseeded operation is forbidden, not open.
        var checker = Checker(Manager, [1001]);

        Assert.False(await checker.HasAsync(Resource, Action.Update, CancellationToken.None));
    }

    [Fact]
    public async Task RowForARoleTheCallerDoesNotHold_Denies()
    {
        var checker = Checker(Manager, [1001], Row(roleId: 5, Action.Update));

        Assert.False(await checker.HasAsync(Resource, Action.Update, CancellationToken.None));
    }

    [Fact]
    public async Task RowForAHeldRole_Grants()
    {
        var checker = Checker(Manager, [1001], Row(roleId: 1001, Action.Update));

        Assert.True(await checker.HasAsync(Resource, Action.Update, CancellationToken.None));
    }

    [Fact]
    public async Task ConstrainedRow_DoesNotGrant()
    {
        // A Constraint is a predicate over an entity; this API has no entity, so treating it as satisfied
        // would invent a grant nobody wrote.
        var checker = Checker(Manager, [1001], Row(roleId: 1001, Action.Update, constraint: "UserId == @0"));

        Assert.False(await checker.HasAsync(Resource, Action.Update, CancellationToken.None));
    }

    [Fact]
    public async Task Root_BypassesTheMatrixEntirely()
    {
        // Break-glass: immune to a bad seed and to admin lockout, matching the entity path.
        var checker = Checker(Manager, [(int)RoleType.Root]);

        Assert.True(await checker.HasAsync(Resource, Action.Delete, CancellationToken.None));
    }

    [Fact]
    public async Task Demand_ThrowsForbiddenWhenDenied()
    {
        var checker = Checker(Manager, [1001]);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => checker.DemandAsync(Resource, Action.Update, CancellationToken.None));
    }

    [Fact]
    public async Task Granted_OrsTheActionBitsOfEveryHeldRow()
    {
        // Two roles granting different halves of the same resource must add up — holding an extra role can
        // only ever widen.
        var checker = Checker(Manager, [1001, 1002],
            Row(roleId: 1001, Action.Read),
            Row(roleId: 1002, Action.Update),
            Row(roleId: 5, Action.Delete));

        var granted = await checker.GrantedAsync(CancellationToken.None);

        Assert.Equal((int)(Action.Read | Action.Update), granted[Resource]);
    }

    [Fact]
    public async Task Granted_ForRoot_IsTheWildcard()
    {
        var checker = Checker(Manager, [(int)RoleType.Root], Row(roleId: 1001, Action.Read));

        var granted = await checker.GrantedAsync(CancellationToken.None);

        Assert.Equal((int)Action.All, granted[IPermissionChecker.AllResources]);
    }
}
