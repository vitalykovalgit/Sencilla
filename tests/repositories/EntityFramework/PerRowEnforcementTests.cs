using Microsoft.Data.Sqlite;
using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Verifies the per-row permission mapping of upsert-style operations: rows whose
/// match key exists are checked as UPDATE, new rows as CREATE, and Merge removals
/// as DELETE — each check skipped entirely when its group is empty.
///
/// The bulk operations themselves emit T-SQL MERGE, which Sqlite cannot execute —
/// allowed paths therefore end in a provider error AFTER the checks. Tests assert
/// either a ForbiddenException thrown BEFORE the store is touched, or the set of
/// events published up to the provider error (Record.ExceptionAsync + event verify).
/// </summary>
public class PerRowEnforcementTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _db;
    private readonly Mock<IEventDispatcher> _events = new();

    public PerRowEnforcementTests()
    {
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

    private async Task Seed(params TestProduct[] products)
    {
        _db.Products.AddRange(products);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private void NarrowUpdating(Func<IQueryable<TestProduct>, IQueryable<TestProduct>> narrow) =>
        _events.Setup(x => x.PublishAsync(It.IsAny<EntityUpdatingEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
               .Returns<EntityUpdatingEvent<TestProduct>, CancellationToken>((e, _) =>
               {
                   e.DbEntities = narrow(e.DbEntities!);
                   return Task.CompletedTask;
               });

    private void NarrowDeleting(Func<IQueryable<TestProduct>, IQueryable<TestProduct>> narrow) =>
        _events.Setup(x => x.PublishAsync(It.IsAny<EntityDeletingEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
               .Returns<EntityDeletingEvent<TestProduct>, CancellationToken>((e, _) =>
               {
                   e.Entities = narrow(e.Entities!);
                   return Task.CompletedTask;
               });

    private void VerifyPublished<TEvent>(Times times) where TEvent : class, IEvent =>
        _events.Verify(x => x.PublishAsync(It.IsAny<TEvent>(), It.IsAny<CancellationToken>()), times);

    /// <summary>Runs an allowed path expected to die at the Sqlite MERGE — asserts it was NOT a denial.</summary>
    private static async Task AssertReachedStore(Func<Task> operation)
    {
        var ex = await Record.ExceptionAsync(operation);
        Assert.NotNull(ex);
        Assert.IsNotType<ForbiddenException>(ex);
    }

    // ── Upsert ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upsert_AllNewRows_ChecksCreateOnly()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        await AssertReachedStore(() => repo.UpsertAsync(new TestProduct { Name = "new" }, x => x.Name));

        VerifyPublished<EntityCreatingEvent<TestProduct>>(Times.Once());
        VerifyPublished<EntityUpdatingEvent<TestProduct>>(Times.Never());
    }

    [Fact]
    public async Task Upsert_AllExistingRows_ChecksUpdateOnly()
    {
        await Seed(new TestProduct { Id = 1, Name = "k1" });
        var repo = new TestCreateRepository(Dependency, _db);

        await AssertReachedStore(() => repo.UpsertAsync(new TestProduct { Name = "k1", Stock = 5 }, x => x.Name));

        VerifyPublished<EntityCreatingEvent<TestProduct>>(Times.Never());
        VerifyPublished<EntityUpdatingEvent<TestProduct>>(Times.Once());
    }

    [Fact]
    public async Task Upsert_MixedBatch_SplitsChecksPerRow()
    {
        await Seed(new TestProduct { Id = 1, Name = "k1" });
        EntityCreatingEvent<TestProduct>? creating = null;
        _events.Setup(x => x.PublishAsync(It.IsAny<EntityCreatingEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
               .Callback<EntityCreatingEvent<TestProduct>, CancellationToken>((e, _) => creating = e)
               .Returns(Task.CompletedTask);
        var repo = new TestCreateRepository(Dependency, _db);

        await AssertReachedStore(() => repo.UpsertAsync(
            new[] { new TestProduct { Name = "k1" }, new TestProduct { Name = "k2" } },
            x => x.Name, null, null));

        VerifyPublished<EntityUpdatingEvent<TestProduct>>(Times.Once());
        Assert.NotNull(creating);
        Assert.Equal("k2", creating!.Entities!.Single().Name);
    }

    [Fact]
    public async Task Upsert_ExistingRowDenied_ForbiddenBeforeWrite()
    {
        await Seed(new TestProduct { Id = 1, Name = "k1", Stock = 10 });
        NarrowUpdating(q => q.Where(p => p.Id < 0));
        var repo = new TestCreateRepository(Dependency, _db);

        // ForbiddenException, not a provider error: denial happens before the MERGE.
        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.UpsertAsync(new TestProduct { Name = "k1", Stock = 0 }, x => x.Name));

        Assert.Equal(10, _db.Products.AsNoTracking().Single().Stock);
    }

    // ── Merge ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Merge_RemovalsDenied_ForbiddenBeforeWrite()
    {
        await Seed(new TestProduct { Id = 1, Name = "kept" }, new TestProduct { Id = 2, Name = "victim" });
        NarrowDeleting(q => q.Where(p => p.Name != "victim"));
        var repo = new TestCreateRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.MergeAsync(new TestProduct { Name = "kept" }, x => x.Name));

        Assert.Equal(2, _db.Products.AsNoTracking().Count());
    }

    [Fact]
    public async Task Merge_NoRemovals_SkipsDeleteCheck()
    {
        await Seed(new TestProduct { Id = 1, Name = "k1" });
        var repo = new TestCreateRepository(Dependency, _db);

        await AssertReachedStore(() => repo.MergeAsync(new TestProduct { Name = "k1" }, x => x.Name));

        VerifyPublished<EntityDeletingEvent<TestProduct>>(Times.Never());
    }

    // ── GetOrCreate ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreate_AllExisting_SkipsCreateCheck()
    {
        await Seed(new TestProduct { Id = 1, Name = "k1" });
        var repo = new TestCreateRepository(Dependency, _db);

        await AssertReachedStore(() => repo.GetOrCreateAsync(new[] { new TestProduct { Name = "k1" } }, "Name"));

        VerifyPublished<EntityCreatingEvent<TestProduct>>(Times.Never());
    }

    [Fact]
    public async Task GetOrCreate_NewRows_ChecksCreate()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        await AssertReachedStore(() => repo.GetOrCreateAsync(new[] { new TestProduct { Name = "k1" } }, "Name"));

        VerifyPublished<EntityCreatingEvent<TestProduct>>(Times.Once());
    }

    // ── JsonMerge ────────────────────────────────────────────────────────────

    [Fact]
    public async Task JsonMerge_Denied_ForbiddenBeforeWrite()
    {
        await Seed(new TestProduct { Id = 1, Name = "k1" });
        NarrowUpdating(q => q.Where(p => p.Id < 0));
        var repo = new TestUpdateRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.JsonMergeAsync(1, p => p.Meta, "key", "value"));
    }

    // ── QueryAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_AppliesReadingPipeline()
    {
        await Seed(new TestProduct { Id = 1 }, new TestProduct { Id = 2 });
        _events.Setup(x => x.PublishAsync(It.IsAny<EntityReadingEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
               .Returns<EntityReadingEvent<TestProduct>, CancellationToken>((e, _) =>
               {
                   e.Entities = e.Entities!.Where(p => p.Id != 2);
                   return Task.CompletedTask;
               });
        var repo = new TestReadRepository(Dependency, _db);

        var constrained = await repo.QueryAsync();
        var raw = repo.Query;

        Assert.Equal(1, constrained.Count());
        Assert.Equal(2, raw.Count());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
