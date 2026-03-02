using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="DynamicDbContext"/> compiled model caching
/// and <see cref="RepositoryEntityFrameworkBootstrap.WarmUpEFModel"/>.
///
/// Covers: model pre-compilation, OnConfiguring with cached model, idempotent warm-up.
/// </summary>
public class DynamicDbContextTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public DynamicDbContextTests()
    {
        // Register DynamicDbContext with InMemory provider
        var services = new ServiceCollection();
        services.AddDbContext<DynamicDbContext>(opts =>
            opts.UseInMemoryDatabase($"DynamicDbTest_{Guid.NewGuid()}"));

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CompileModel_ResolvesDbContextAndCachesModel()
    {
        // Act — should not throw
        DynamicDbContext.CompileModel(_serviceProvider);

        // Verify a context can still be resolved after compilation
        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<DynamicDbContext>();
        Assert.NotNull(ctx);
        Assert.NotNull(ctx.Model);
    }

    [Fact]
    public void CompileModel_IsIdempotent()
    {
        // Calling twice should not throw
        DynamicDbContext.CompileModel(_serviceProvider);
        DynamicDbContext.CompileModel(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<DynamicDbContext>();
        Assert.NotNull(ctx.Model);
    }

    [Fact]
    public void WarmUpEFModel_ExtensionMethod_Works()
    {
        var host = new Mock<IHost>();
        host.Setup(h => h.Services).Returns(_serviceProvider);

        // Act — returns self for fluent chaining
        var result = host.Object.WarmUpEFModel();

        Assert.Same(host.Object, result);
    }

    public void Dispose() => _serviceProvider.Dispose();
}
