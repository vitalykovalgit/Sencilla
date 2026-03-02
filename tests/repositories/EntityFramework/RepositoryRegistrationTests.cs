using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="RepoEFIServiceCollectionEx"/> to verify
/// repositories are registered with Scoped lifetime.
///
/// Covers: Read, Create, Update, Remove, Delete repository registration,
///         both generic (TKey) and int-shorthand variants,
///         filter constraint handler registration.
/// </summary>
public class RepositoryRegistrationTests
{
    private static IServiceCollection CreateServicesWithTestProduct()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DynamicDbContext>(opts =>
            opts.UseInMemoryDatabase($"RegTest_{Guid.NewGuid()}"));

        services.RegisterEFRepositoriesForType(typeof(TestProduct), out _);
        services.RegisterEFFilters(typeof(TestProduct));
        return services;
    }

    private static ServiceLifetime? GetLifetime<TService>(IServiceCollection services)
    {
        return services.FirstOrDefault(d => d.ServiceType == typeof(TService))?.Lifetime;
    }

    // ── Read Repository ──────────────────────────────────────────────────────

    [Fact]
    public void ReadRepository_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IReadRepository<TestProduct, int>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void ReadRepository_IntShorthand_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IReadRepository<TestProduct>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    // ── Create Repository ────────────────────────────────────────────────────

    [Fact]
    public void CreateRepository_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<ICreateRepository<TestProduct, int>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void CreateRepository_IntShorthand_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<ICreateRepository<TestProduct>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    // ── Update Repository ────────────────────────────────────────────────────

    [Fact]
    public void UpdateRepository_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IUpdateRepository<TestProduct, int>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void UpdateRepository_IntShorthand_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IUpdateRepository<TestProduct>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    // ── Remove Repository ────────────────────────────────────────────────────

    [Fact]
    public void RemoveRepository_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IRemoveRepository<TestProduct, int>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void RemoveRepository_IntShorthand_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IRemoveRepository<TestProduct>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    // ── Delete Repository ────────────────────────────────────────────────────

    [Fact]
    public void DeleteRepository_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IDeleteRepository<TestProduct, int>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void DeleteRepository_IntShorthand_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();
        var lifetime = GetLifetime<IDeleteRepository<TestProduct>>(services);

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    // ── Filter Constraint Handler ────────────────────────────────────────────

    [Fact]
    public void FilterConstraintHandler_IsRegistered_AsScoped()
    {
        var services = CreateServicesWithTestProduct();

        var handlerType = typeof(IEventHandlerBase<>).MakeGenericType(
            typeof(EntityReadingEvent<>).MakeGenericType(typeof(TestProduct)));

        var lifetime = services.FirstOrDefault(d => d.ServiceType == handlerType)?.Lifetime;

        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    // ── Registration completeness ────────────────────────────────────────────

    [Fact]
    public void RegisterEFRepositoriesForType_SetsIsAddedTrue_ForValidEntity()
    {
        var services = new ServiceCollection();
        services.RegisterEFRepositoriesForType(typeof(TestProduct), out var isAdded);

        Assert.True(isAdded);
    }

    [Fact]
    public void RegisterEFRepositoriesForType_SetsIsAddedFalse_ForNonEntity()
    {
        var services = new ServiceCollection();
        services.RegisterEFRepositoriesForType(typeof(string), out var isAdded);

        Assert.False(isAdded);
    }
}
