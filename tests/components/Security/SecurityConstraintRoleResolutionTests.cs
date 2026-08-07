using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Security;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

/// <summary>
/// Role resolution used to live protected inside <see cref="SecurityConstraintHandler{TEntity}"/> and was
/// tested through a subclass. It now lives in <see cref="UserRoleResolver"/> so that entity enforcement and
/// endpoint enforcement share one answer — the tests moved with it, unchanged in intent.
/// </summary>
public class SecurityConstraintRoleResolutionTests
{
    private static UserRoleResolver Resolver(IRoleClosure? closure = null) => new(
        Mock.Of<ISystemVariable>(),
        new MemoryCache(new MemoryCacheOptions()),
        Mock.Of<IReadRepository<UserRole, Guid>>(),
        new SecurityCacheSignal(),
        closure ?? Mock.Of<IRoleClosure>());

    [Fact]
    public void NullUser_GetsAnonymousOnly()
    {
        var roles = Resolver().Resolve(null);
        Assert.Equal(new HashSet<int> { (int)RoleType.Anonymous }, roles);
    }

    [Fact]
    public void AnonymousUser_GetsAnonymousOnly()
    {
        // No email and phone 0 → IsAnonymous().
        var roles = Resolver().Resolve(new User());
        Assert.Equal(new HashSet<int> { (int)RoleType.Anonymous }, roles);
    }

    [Fact]
    public void AuthenticatedUser_WithoutPersistedId_GetsUserRole()
    {
        // First-login self-registration: verified identity (email) but no DB Id yet.
        // Regression guard — previously the empty Id stripped the User role, leaving
        // only Anonymous, which forbade the registration insert (403).
        var user = new User { Email = "new@example.com", Id = Guid.Empty };

        var roles = Resolver().Resolve(user);

        Assert.Contains((int)RoleType.Anonymous, roles);
        Assert.Contains((int)RoleType.User, roles);
    }

    [Fact]
    public void Resolve_DoesNotExpandInheritance()
    {
        // The Root break-glass check reads the DIRECT set. If Resolve expanded first, any role whose parent
        // chain reached Root would silently BE Root. Expansion is ResolveExpanded's job alone.
        var closure = new Mock<IRoleClosure>();
        closure.Setup(c => c.Expand(It.IsAny<HashSet<int>>())).Returns([9999]);

        var roles = Resolver(closure.Object).Resolve(new User { Email = "a@b.c", Id = Guid.Empty });

        Assert.DoesNotContain(9999, roles);
        closure.Verify(c => c.Expand(It.IsAny<HashSet<int>>()), Times.Never);
    }

    [Fact]
    public void ResolveExpanded_RunsTheClosure()
    {
        var closure = new Mock<IRoleClosure>();
        closure.Setup(c => c.Expand(It.IsAny<HashSet<int>>())).Returns([(int)RoleType.Anonymous, 1001]);

        var roles = Resolver(closure.Object).ResolveExpanded(null);

        Assert.Contains(1001, roles);
    }
}
