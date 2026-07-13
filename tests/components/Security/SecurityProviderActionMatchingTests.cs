using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class SecurityProviderActionMatchingTests
{
    private static SecurityProvider CreateProvider(params Matrix[] rows)
    {
        var declaration = new Mock<ISecurityDeclaration>();
        declaration.Setup(d => d.Permissions(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(rows);

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<ISecurityDeclaration>)))
                .Returns(new[] { declaration.Object });

        return new SecurityProvider(services.Object, new MemoryCache(new MemoryCacheOptions()), new SecurityCacheSignal());
    }

    private static Matrix Row(Action action) => new()
    {
        RoleId = (int)RoleType.User,
        Resource = "Order",
        Action = (int)action,
    };

    [Theory]
    [InlineData(Action.Read)]
    [InlineData(Action.Create)]
    [InlineData(Action.Update)]
    [InlineData(Action.Delete)]
    public async Task AllRow_GrantsEveryAction(Action requested)
    {
        // Regression guard — with the old sequential values (All=5) the bitwise
        // match silently denied Create and Update for '*'/All rows.
        var provider = CreateProvider(Row(Action.All));

        var permissions = await provider.Permissions(CancellationToken.None, "Order", requested);

        Assert.Single(permissions);
    }

    [Theory]
    [InlineData(Action.Read)]
    [InlineData(Action.Create)]
    [InlineData(Action.Delete)]
    public async Task UpdateRow_GrantsOnlyUpdate(Action requested)
    {
        // Regression guard — old Update=3 accidentally matched Read and Create.
        var provider = CreateProvider(Row(Action.Update));

        var permissions = await provider.Permissions(CancellationToken.None, "Order", requested);

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task CombinedRow_GrantsEachContainedAction()
    {
        var provider = CreateProvider(Row(Action.Read | Action.Update));

        Assert.Single(await provider.Permissions(CancellationToken.None, "Order", Action.Read));
        Assert.Single(await provider.Permissions(CancellationToken.None, "Order", Action.Update));
        Assert.Empty(await provider.Permissions(CancellationToken.None, "Order", Action.Delete));
    }

    [Fact]
    public async Task NoActionFilter_ReturnsAllRowsForResource()
    {
        var provider = CreateProvider(Row(Action.Read), Row(Action.Delete));

        var permissions = await provider.Permissions(CancellationToken.None, "Order");

        Assert.Equal(2, permissions.Count());
    }
}
