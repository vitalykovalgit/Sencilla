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
        // No account id → IsAnonymous().
        var roles = Resolver().Resolve(new User());
        Assert.Equal(new HashSet<int> { (int)RoleType.Anonymous }, roles);
    }

    [Fact]
    public void AnEmailWithoutAnAccountId_IsStillTheVisitor()
    {
        // Since the session carries an account id, a contact alone is nobody's account.
        var user = new User { Email = "new@example.com", Phone = 380501234567, Id = Guid.Empty };

        Assert.True(user.IsAnonymous());
        Assert.Equal(new HashSet<int> { (int)RoleType.Anonymous }, Resolver().Resolve(user));
    }

    [Fact]
    public void AnAccountWithNeitherAnEmailNorAPhone_GetsTheUserRole()
    {
        // A social sign-in that gave no contact (Instagram, Telegram without the phone) is still an account.
        var user = new User { Id = Guid.NewGuid() };

        var roles = Resolver().Resolve(user);

        Assert.False(user.IsAnonymous());
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

        var roles = Resolver(closure.Object).Resolve(new User { Email = "a@b.c", Id = Guid.NewGuid() });

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
