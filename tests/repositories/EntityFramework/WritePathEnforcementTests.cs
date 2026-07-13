using Microsoft.Data.Sqlite;
using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Verifies the write-path enforcement choreography: constraint handlers narrow the
/// database query carried by the -ing/-ed events, and the repository applies
/// all-or-nothing denial (ForbiddenException, nothing persisted) when any existing
/// target row is lost — while ids that never existed are not treated as denied.
/// Uses Sqlite so pre-/post-image queries and rollback are real.
/// </summary>
public class WritePathEnforcementTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _db;
    private readonly Mock<IEventDispatcher> _events = new();

    public WritePathEnforcementTests()
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

    // Constraint handlers compose into the event's DB query with Where() — these
    // helpers mimic exactly what SecurityConstraintHandler does.
    private void NarrowUpdating(Func<IQueryable<TestProduct>, IQueryable<TestProduct>> narrow) =>
        _events.Setup(x => x.PublishAsync(It.IsAny<EntityUpdatingEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
               .Returns<EntityUpdatingEvent<TestProduct>, CancellationToken>((e, _) =>
               {
                   e.DbEntities = narrow(e.DbEntities!);
                   return Task.CompletedTask;
               });

    private void NarrowUpdated(Func<IQueryable<TestProduct>, IQueryable<TestProduct>> narrow) =>
        _events.Setup(x => x.PublishAsync(It.IsAny<EntityUpdatedEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
               .Returns<EntityUpdatedEvent<TestProduct>, CancellationToken>((e, _) =>
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

    private TestProduct DbRow(int id) => _db.Products.AsNoTracking().Single(p => p.Id == id);

    // ── Update: pre-image ────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Forbidden_WhenPreImageViolatesConstraint()
    {
        // The DB row does not satisfy the constraint — a forged client object
        // (which would satisfy it) must not get the update through.
        await Seed(new TestProduct { Id = 1, Name = "theirs" });
        NarrowUpdating(q => q.Where(p => p.Name == "mine"));
        var repo = new TestUpdateRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.Update(new TestProduct { Id = 1, Name = "mine" }));

        Assert.Equal("theirs", DbRow(1).Name);
    }

    [Fact]
    public async Task Update_Succeeds_WhenPreImageSatisfiesConstraint()
    {
        await Seed(new TestProduct { Id = 1, Name = "mine", Stock = 10 });
        NarrowUpdating(q => q.Where(p => p.Name == "mine"));
        var repo = new TestUpdateRepository(Dependency, _db);

        await repo.Update(new TestProduct { Id = 1, Name = "mine", Stock = 20 });

        Assert.Equal(20, DbRow(1).Stock);
    }

    // ── Update: post-image ───────────────────────────────────────────────────

    [Fact]
    public async Task Update_RollsBack_WhenPostImageViolatesConstraint()
    {
        // The write itself succeeds, but the resulting row falls out of the
        // constraint scope (giving the row away) — must roll back.
        await Seed(new TestProduct { Id = 1, Name = "kept", Stock = 10 });
        NarrowUpdated(q => q.Where(p => p.Stock > 0));
        var repo = new TestUpdateRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.Update(new TestProduct { Id = 1, Name = "kept", Stock = 0 }));

        Assert.Equal(10, DbRow(1).Stock);
    }

    // ── Delete by ids ────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Forbidden_WhenAnyExistingRowDenied_NothingDeleted()
    {
        await Seed(new TestProduct { Id = 1 }, new TestProduct { Id = 2 });
        NarrowDeleting(q => q.Where(p => p.Id != 2));
        var repo = new TestDeleteRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(() => repo.Delete(new[] { 1, 2 }));

        Assert.Equal(2, _db.Products.AsNoTracking().Count());
    }

    [Fact]
    public async Task Delete_NonexistentIds_AreNotDenied()
    {
        await Seed(new TestProduct { Id = 1 });
        NarrowDeleting(q => q); // constraint present but not narrowing
        var repo = new TestDeleteRepository(Dependency, _db);

        var count = await repo.Delete(new[] { 1, 999 });

        Assert.Equal(1, count);
        Assert.Equal(0, _db.Products.AsNoTracking().Count());
    }

    [Fact]
    public async Task Delete_ExecutesThroughNarrowedQuery()
    {
        // The constraint is part of the DELETE's WHERE: rows outside it survive
        // even though they match the requested ids' table.
        await Seed(new TestProduct { Id = 1 }, new TestProduct { Id = 2 });
        NarrowDeleting(q => q.Where(p => p.Id != 2));
        var repo = new TestDeleteRepository(Dependency, _db);

        var count = await repo.Delete(new[] { 1 });

        Assert.Equal(1, count);
        Assert.NotNull(_db.Products.AsNoTracking().SingleOrDefault(p => p.Id == 2));
    }

    [Fact]
    public async Task Delete_Entities_Forbidden_WhenDenied()
    {
        await Seed(new TestProduct { Id = 1, Name = "protected" });
        NarrowDeleting(q => q.Where(p => p.Name != "protected"));
        var repo = new TestDeleteRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.Delete(new[] { new TestProduct { Id = 1, Name = "protected" } }));

        Assert.Equal(1, _db.Products.AsNoTracking().Count());
    }

    // ── Soft delete: Remove/Undo map to the Delete action ────────────────────

    [Fact]
    public async Task Remove_PublishesDeleteEvents_NotUpdateEvents()
    {
        await Seed(new TestProduct { Id = 1 });
        var repo = new TestRemoveRepository(Dependency, _db);

        await repo.Remove(new TestProduct { Id = 1 });

        Assert.NotNull(DbRow(1).DeletedDate);
        _events.Verify(x => x.PublishAsync(It.IsAny<EntityDeletingEvent<TestProduct>>(), It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(x => x.PublishAsync(It.IsAny<EntityDeletedEvent<TestProduct>>(), It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(x => x.PublishAsync(It.IsAny<EntityUpdatingEvent<TestProduct>>(), It.IsAny<CancellationToken>()), Times.Never);
        _events.Verify(x => x.PublishAsync(It.IsAny<EntityUpdatedEvent<TestProduct>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Remove_Forbidden_WhenDeleteConstraintDenies()
    {
        await Seed(new TestProduct { Id = 1, Name = "protected" });
        NarrowDeleting(q => q.Where(p => p.Name != "protected"));
        var repo = new TestRemoveRepository(Dependency, _db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.Remove(new TestProduct { Id = 1, Name = "protected" }));

        Assert.Null(DbRow(1).DeletedDate);
    }

    [Fact]
    public async Task Undo_RestoresRow_UnderDeleteAction()
    {
        await Seed(new TestProduct { Id = 1, DeletedDate = DateTime.UtcNow });
        var repo = new TestRemoveRepository(Dependency, _db);

        await repo.Undo(new TestProduct { Id = 1, DeletedDate = DateTime.UtcNow });

        Assert.Null(DbRow(1).DeletedDate);
        _events.Verify(x => x.PublishAsync(It.IsAny<EntityDeletingEvent<TestProduct>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
