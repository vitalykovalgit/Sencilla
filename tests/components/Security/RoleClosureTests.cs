using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class RoleClosureTests
{
    private static RoleClosure Closure(params Role[] roles)
    {
        var repo = new Mock<IReadRepository<Role, int>>();
        repo.Setup(r => r.Query).Returns(roles.AsQueryable());
        return new RoleClosure(repo.Object, new MemoryCache(new MemoryCacheOptions()), new SecurityCacheSignal());
    }

    private static Role R(int id, int? parent = null) =>
        new() { Id = id, Name = $"role-{id}", ParentId = parent };

    [Fact]
    public void Expand_WalksParentChainTransitively()
    {
        // photographer(102) -> manager(101) -> user(3)
        var closure = Closure(R(3), R(101, parent: 3), R(102, parent: 101));

        var expanded = closure.Expand([102]);

        Assert.Equal(new HashSet<int> { 102, 101, 3 }, expanded);
    }

    [Fact]
    public void Expand_KeepsUnknownRoles()
    {
        var closure = Closure(R(3));

        var expanded = closure.Expand([3, 999]);

        Assert.Equal(new HashSet<int> { 3, 999 }, expanded);
    }

    [Fact]
    public void Expand_ToleratesPreexistingCycle()
    {
        var closure = Closure(R(101, parent: 102), R(102, parent: 101));

        var expanded = closure.Expand([101]);

        Assert.Equal(new HashSet<int> { 101, 102 }, expanded);
    }

    [Fact]
    public void GrantorsOf_IsReverseClosure()
    {
        // Owner(6) ⊃ Editor(7) ⊃ Viewer(8): holding Owner activates Editor/Viewer rows.
        var closure = Closure(R(6, parent: 7), R(7, parent: 8), R(8));

        Assert.Equal(new HashSet<int> { 6 }, closure.GrantorsOf(6));
        Assert.Equal(new HashSet<int> { 6, 7 }, closure.GrantorsOf(7));
        Assert.Equal(new HashSet<int> { 6, 7, 8 }, closure.GrantorsOf(8));
    }

    [Fact]
    public void GrantorsOf_UnknownRole_IsItself()
    {
        var closure = Closure(R(6));

        Assert.Equal(new HashSet<int> { 999 }, closure.GrantorsOf(999));
    }

    [Fact]
    public void Exists_ReflectsSnapshot()
    {
        var closure = Closure(R(6));

        Assert.True(closure.Exists(6));
        Assert.False(closure.Exists(999));
    }

    [Fact]
    public void WouldCreateCycle_DetectsIndirectCycle()
    {
        // 102 -> 101; making 101's parent 102 closes the loop.
        var closure = Closure(R(101), R(102, parent: 101));

        Assert.True(closure.WouldCreateCycle(101, 102));
        Assert.False(closure.WouldCreateCycle(102, 101));
    }
}
