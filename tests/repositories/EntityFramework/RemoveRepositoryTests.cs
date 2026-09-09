using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="RemoveRepository{TEntity,TContext}"/>.
///
/// Covers: Remove single (soft delete — sets DeletedDate), Remove bulk,
/// Undo single (clears DeletedDate), Undo bulk.
/// The row is NOT removed from the database; only DeletedDate is set/cleared.
/// </summary>
public class RemoveRepositoryTests : RepositoryTestBase<TestRemoveRepository>
{
    protected override TestRemoveRepository CreateRepository(RepositoryDependency dep, TestDbContext ctx)
        => new(dep, ctx);

    // ── Remove single ────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_SingleEntity_SetsDeletedDate()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Repository.Remove(product!);

        DbContext.ChangeTracker.Clear();
        var removed = await DbContext.Products.FindAsync(1);
        Assert.NotNull(removed!.DeletedDate);
        Assert.True(removed.DeletedDate >= before);
        Assert.True(removed.DeletedDate <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Remove_SingleEntity_DoesNotDeleteRowFromDatabase()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);

        await Repository.Remove(product!);

        DbContext.ChangeTracker.Clear();
        Assert.NotNull(await DbContext.Products.FindAsync(1));
    }

    [Fact]
    public async Task Remove_SingleEntity_ReturnsEntityWithDeletedDate()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);

        var result = await Repository.Remove(product!);

        Assert.NotNull(result);
        Assert.NotNull(result.DeletedDate);
    }

    // ── The stored row, not the caller's object ──────────────────────────────

    /// <summary>
    /// A caller sends little more than the id — a client's delete is <c>new Project({ id })</c> —
    /// and a soft delete changes DeletedDate and nothing else. Persisting the caller's object used to
    /// replace the row with whatever the caller carried: an FK violation at best (UserId = Guid.Empty,
    /// a 500 for every delete from the cabinet), a silently wiped row at worst, resurrected blank by Undo.
    /// </summary>
    [Fact]
    public async Task Remove_DetachedObjectWithOtherValues_StampsTheStoredRowAndKeepsItsColumns()
    {
        await SeedAsync(MakeProduct(1, name: "Kept", price: 42m, stock: 7));

        // Same id, every other column different — what a stub or a stale client row looks like.
        var result = await Repository.Remove(MakeProduct(1));

        DbContext.ChangeTracker.Clear();
        var removed = await DbContext.Products.FindAsync(1);
        Assert.NotNull(removed!.DeletedDate);
        Assert.Equal("Kept", removed.Name);
        Assert.Equal(42m, removed.Price);
        Assert.Equal(7, removed.Stock);
        Assert.Equal("Kept", result!.Name);
    }

    // ── Remove bulk ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_MultipleEntities_SetsDeletedDateOnAll()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2), MakeProduct(3));
        var products = DbContext.Products.ToList();
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Repository.Remove(products);

        DbContext.ChangeTracker.Clear();
        var all = DbContext.Products.ToList();
        Assert.All(all, p =>
        {
            Assert.NotNull(p.DeletedDate);
            Assert.True(p.DeletedDate >= before);
        });
    }

    [Fact]
    public async Task Remove_MultipleEntities_NoRowsDeletedFromDatabase()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2));
        var products = DbContext.Products.ToList();

        await Repository.Remove(products);

        DbContext.ChangeTracker.Clear();
        Assert.Equal(2, DbContext.Products.Count());
    }

    [Fact]
    public async Task Remove_MultipleEntities_ReturnsAllEntitiesWithDeletedDate()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2));
        var products = DbContext.Products.ToList();

        var result = (await Repository.Remove(products)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.NotNull(p.DeletedDate));
    }

    // ── Undo single ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Undo_SingleEntity_ClearsDeletedDate()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);
        await Repository.Remove(product!);

        DbContext.ChangeTracker.Clear();
        var removed = await DbContext.Products.FindAsync(1);
        await Repository.Undo(removed!);

        DbContext.ChangeTracker.Clear();
        var restored = await DbContext.Products.FindAsync(1);
        Assert.Null(restored!.DeletedDate);
    }

    [Fact]
    public async Task Undo_SingleEntity_ReturnsEntityWithNullDeletedDate()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);
        await Repository.Remove(product!);

        DbContext.ChangeTracker.Clear();
        var removed = await DbContext.Products.FindAsync(1);
        var result = await Repository.Undo(removed!);

        Assert.NotNull(result);
        Assert.Null(result.DeletedDate);
    }

    // ── Undo bulk ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Undo_MultipleEntities_ClearsDeletedDateOnAll()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2));
        var products = DbContext.Products.ToList();
        await Repository.Remove(products);

        DbContext.ChangeTracker.Clear();
        var removed = DbContext.Products.ToList();
        await Repository.Undo(removed);

        DbContext.ChangeTracker.Clear();
        var restored = DbContext.Products.ToList();
        Assert.All(restored, p => Assert.Null(p.DeletedDate));
    }

    [Fact]
    public async Task Undo_MultipleEntities_ReturnsAllEntitiesWithNullDeletedDate()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2), MakeProduct(3));
        var products = DbContext.Products.ToList();
        await Repository.Remove(products);

        DbContext.ChangeTracker.Clear();
        var removed = DbContext.Products.ToList();
        var result = (await Repository.Undo(removed)).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, p => Assert.Null(p.DeletedDate));
    }
}
