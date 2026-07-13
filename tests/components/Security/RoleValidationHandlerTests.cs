using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class RoleValidationHandlerTests
{
    private readonly RoleValidationHandler _handler = new();
    private readonly IRoleClosure _closure;

    public RoleValidationHandlerTests()
    {
        var repo = new Mock<IReadRepository<Role, int>>();
        repo.Setup(r => r.Query).Returns(new[]
        {
            new Role { Id = 3, Name = "User" },
            new Role { Id = 5, Name = "Admin" },
            new Role { Id = 6, Name = "Owner", ParentId = 7 },
            new Role { Id = 7, Name = "Editor" },
            new Role { Id = 101, Name = "Manager", ParentId = 3 },
        }.AsQueryable());
        _closure = new RoleClosure(repo.Object, new MemoryCache(new MemoryCacheOptions()), new SecurityCacheSignal());
    }

    [Fact]
    public async Task Role_MissingParent_IsRejected()
    {
        var e = new EntityCreatingEvent<Role> { Entities = new[] { new Role { Id = 102, ParentId = 999 } }.AsQueryable() };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(e, _closure, CancellationToken.None));
    }

    [Fact]
    public async Task Role_SelfParent_IsRejected()
    {
        var e = new EntityCreatingEvent<Role> { Entities = new[] { new Role { Id = 102, Name = "X", ParentId = 102 } }.AsQueryable() };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(e, _closure, CancellationToken.None));
    }

    [Fact]
    public async Task Role_CycleViaUpdate_IsRejected()
    {
        // Manager(101) already inherits User(3); pointing User at Manager closes the loop.
        var e = new EntityUpdatingEvent<Role> { Entities = new[] { new Role { Id = 3, Name = "User", ParentId = 101 } }.AsQueryable() };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(e, _closure, CancellationToken.None));
    }

    [Fact]
    public async Task Role_ValidParent_Passes()
    {
        var e = new EntityCreatingEvent<Role> { Entities = new[] { new Role { Id = 102, Name = "Photographer", ParentId = 101 } }.AsQueryable() };

        await _handler.HandleAsync(e, _closure, CancellationToken.None);
    }

    [Fact]
    public async Task Role_CrossFormerKindParent_NowPasses()
    {
        // The System/Resource split is gone: Admin(5) inheriting Owner(6) means global
        // admins also activate every Owner row — a coherent, intentional capability.
        var e = new EntityUpdatingEvent<Role> { Entities = new[] { new Role { Id = 5, Name = "Admin", ParentId = 6 } }.AsQueryable() };

        await _handler.HandleAsync(e, _closure, CancellationToken.None);
    }
}
