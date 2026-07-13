using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class SecurityCacheEvictionTests
{
    private readonly List<Matrix> _rows = [new() { RoleId = (int)RoleType.User, Resource = "Order", Action = (int)Action.Read }];
    private readonly SecurityCacheSignal _signal = new();
    private readonly SecurityProvider _provider;

    public SecurityCacheEvictionTests()
    {
        var declaration = new Mock<ISecurityDeclaration>();
        declaration.Setup(d => d.Permissions(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(() => _rows.ToArray());

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<ISecurityDeclaration>)))
                .Returns(new[] { declaration.Object });

        _provider = new SecurityProvider(services.Object, new MemoryCache(new MemoryCacheOptions()), _signal);
    }

    private async Task<int> Count() =>
        (await _provider.Permissions(CancellationToken.None, "Order")).Count();

    [Fact]
    public async Task Permissions_AreCached_UntilSignalInvalidates()
    {
        Assert.Equal(1, await Count());

        _rows.Add(new Matrix { RoleId = (int)RoleType.User, Resource = "Order", Action = (int)Action.Create });

        // Still served from cache — the declaration is not re-queried.
        Assert.Equal(1, await Count());

        _signal.Invalidate();

        Assert.Equal(2, await Count());
    }

    [Theory]
    [InlineData(0)] // Matrix
    [InlineData(1)] // Role
    [InlineData(2)] // UserRole (payload-free delete — the reason eviction is signal-wide)
    public async Task EvictionHandler_RefreshesPermissions_OnSecurityEntityChange(int entity)
    {
        Assert.Equal(1, await Count());
        _rows.Add(new Matrix { RoleId = (int)RoleType.User, Resource = "Order", Action = (int)Action.Create });

        var handler = new SecurityCacheEvictionHandler();
        await (entity switch
        {
            0 => handler.HandleAsync(new EntityDeletedEvent<Matrix>(), _signal, CancellationToken.None),
            1 => handler.HandleAsync(new EntityUpdatedEvent<Role>(), _signal, CancellationToken.None),
            _ => handler.HandleAsync(new EntityDeletedEvent<UserRole>(), _signal, CancellationToken.None),
        });

        Assert.Equal(2, await Count());
    }
}
