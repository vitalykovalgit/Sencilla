using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Security;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class SecurityConstraintRoleResolutionTests
{
    // ResolveRoleIds is protected; expose it via a concrete subclass for testing.
    private sealed class Handler : SecurityConstraintHandler<User>
    {
        public HashSet<int> Resolve(User? user) => ResolveRoleIds(
            user,
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IReadRepository<UserRole, Guid>>());
    }

    private readonly Handler _handler = new();

    [Fact]
    public void NullUser_GetsAnonymousOnly()
    {
        var roles = _handler.Resolve(null);
        Assert.Equal(new HashSet<int> { (int)RoleType.Anonymous }, roles);
    }

    [Fact]
    public void AnonymousUser_GetsAnonymousOnly()
    {
        // No email and phone 0 → IsAnonymous().
        var roles = _handler.Resolve(new User());
        Assert.Equal(new HashSet<int> { (int)RoleType.Anonymous }, roles);
    }

    [Fact]
    public void AuthenticatedUser_WithoutPersistedId_GetsUserRole()
    {
        // First-login self-registration: verified identity (email) but no DB Id yet.
        // Regression guard — previously the empty Id stripped the User role, leaving
        // only Anonymous, which forbade the registration insert (403).
        var user = new User { Email = "new@example.com", Id = Guid.Empty };

        var roles = _handler.Resolve(user);

        Assert.Contains((int)RoleType.Anonymous, roles);
        Assert.Contains((int)RoleType.User, roles);
    }
}
