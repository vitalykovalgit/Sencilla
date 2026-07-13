using Microsoft.Data.Sqlite;
using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Verifies the ambient write transaction: the -ing event, the save and the -ed
/// handlers commit or roll back as one unit. Uses Sqlite in-memory because the
/// InMemory provider has no transaction support (repositories skip the ambient
/// transaction there — covered implicitly by every other suite in this project).
/// </summary>
public class AmbientTransactionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _db;
    private readonly Mock<IEventDispatcher> _events = new();

    public AmbientTransactionTests()
    {
        // Sqlite :memory: lives as long as this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestDbContext(options);
        _db.Database.EnsureCreated();
    }

    private RepositoryDependency Dependency => new(
        Mock.Of<IServiceProvider>(), _events.Object, Mock.Of<ICommandDispatcher>());

    private void ThrowOn<TEvent>() where TEvent : class, IEvent =>
        _events
            .Setup(x => x.PublishAsync(It.IsAny<TEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));

    private int CountInDb() => _db.Products.AsNoTracking().Count();

    [Fact]
    public async Task Create_Commits_WhenAllHandlersSucceed()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        await repo.Create(new TestProduct { Name = "kept" });

        Assert.Equal(1, CountInDb());
    }

    [Fact]
    public async Task Create_RollsBackInsert_WhenCreatedHandlerThrows()
    {
        // Decision: the -ed handlers run inside the ambient transaction, so a failing
        // handler (e.g. owner-row provisioning) must also revert the saved entity.
        ThrowOn<EntityCreatedEvent<TestProduct>>();
        var repo = new TestCreateRepository(Dependency, _db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.Create(new TestProduct { Name = "ghost" }));

        Assert.Equal(0, CountInDb());
    }

    [Fact]
    public async Task Update_RollsBackChange_WhenUpdatedHandlerThrows()
    {
        _db.Products.Add(new TestProduct { Id = 1, Name = "original" });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        ThrowOn<EntityUpdatedEvent<TestProduct>>();
        var repo = new TestUpdateRepository(Dependency, _db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.Update(new TestProduct { Id = 1, Name = "mutated" }));

        Assert.Equal("original", _db.Products.AsNoTracking().Single(p => p.Id == 1).Name);
    }

    [Fact]
    public async Task Create_ReusesCallerTransaction_CallerOwnsOutcome()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        await using (var transaction = await repo.BeginTransaction())
        {
            // Must not throw "already in a transaction" — the ambient wrapper
            // detects the open transaction and joins it instead of nesting.
            await repo.Create(new TestProduct { Name = "caller-owned" });
            await transaction.RollbackAsync();
        }

        Assert.Equal(0, CountInDb());
    }

    [Fact]
    public async Task Delete_RemovesRow_InsideAmbientTransaction()
    {
        _db.Products.Add(new TestProduct { Id = 1, Name = "doomed" });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var repo = new TestDeleteRepository(Dependency, _db);
        var count = await repo.Delete(1);

        Assert.Equal(1, count);
        Assert.Equal(0, CountInDb());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
