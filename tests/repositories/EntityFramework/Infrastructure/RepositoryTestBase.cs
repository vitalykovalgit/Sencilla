using Microsoft.EntityFrameworkCore;

namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

/// <summary>
/// Abstract base for all EF Core repository tests.
///
/// Provides:
///   - A unique in-memory TestDbContext per test class (no cross-test pollution)
///   - A Moq IEventDispatcher wired to FilterConstraintHandler so IFilter actually works
///   - A no-op ICommandDispatcher mock
///   - Helper methods to seed data directly via DbContext
/// </summary>
public abstract class RepositoryTestBase<TRepo> : IDisposable
    where TRepo : class
{
    protected TestDbContext DbContext { get; }
    protected Mock<IEventDispatcher> EventDispatcherMock { get; }
    protected TRepo Repository { get; }

    protected RepositoryTestBase()
    {
        // Each test class gets a unique in-memory database — no interference between suites.
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        DbContext = new TestDbContext(options);

        EventDispatcherMock = new Mock<IEventDispatcher>();

        // Route EntityReadingEvent through FilterConstraintHandler so that IFilter
        // (pagination, sorting, property filters) is applied correctly in tests.
        EventDispatcherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EntityReadingEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()))
            .Returns<EntityReadingEvent<TestProduct>, CancellationToken>((e, t) =>
                new FilterConstraintHandler<TestProduct>().HandleAsync(e, t));

        // Entity lifecycle events — no-op (tested separately via Verify calls).
        EventDispatcherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EntityCreatingEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        EventDispatcherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EntityCreatedEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        EventDispatcherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EntityUpdatingEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        EventDispatcherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EntityUpdatedEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var commandDispatcherMock = new Mock<ICommandDispatcher>();
        var serviceProviderMock   = new Mock<IServiceProvider>();

        var dependency = new RepositoryDependency(
            serviceProviderMock.Object,
            EventDispatcherMock.Object,
            commandDispatcherMock.Object);

        Repository = CreateRepository(dependency, DbContext);
    }

    /// <summary>Factory method — each derived class creates the specific repository under test.</summary>
    protected abstract TRepo CreateRepository(RepositoryDependency dep, TestDbContext ctx);

    // ── Seed helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts products directly into the DbContext, bypassing the repository layer.
    /// Use this to set up pre-conditions for read/update/delete/remove tests.
    /// </summary>
    protected async Task SeedAsync(params TestProduct[] products)
    {
        DbContext.Products.AddRange(products);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// Creates a product with sensible defaults.
    /// Override individual properties as needed.
    /// </summary>
    protected static TestProduct MakeProduct(
        int id = 0,
        string name = "Test Product",
        decimal price = 9.99m,
        int stock = 10) =>
        new() { Id = id, Name = name, Price = price, Stock = stock };

    public void Dispose() => DbContext.Dispose();
}
