using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="UpdateRepository{TEntity,TContext}"/>.
///
/// Covers: Update single, Update bulk, UpdatedDate auto-set,
/// Detach, ClearChangeTracker,
/// EntityUpdatingEvent and EntityUpdatedEvent published.
/// </summary>
public class UpdateRepositoryTests : RepositoryTestBase<TestUpdateRepository>
{
    protected override TestUpdateRepository CreateRepository(RepositoryDependency dep, TestDbContext ctx)
        => new(dep, ctx);

    // ── Update single ────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_SingleEntity_PersistsChangesToDatabase()
    {
        await SeedAsync(MakeProduct(1, "Old Name", 10m));

        var product = await DbContext.Products.FindAsync(1);
        product!.Name = "New Name";
        product.Price = 99m;

        await Repository.Update(product);

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Products.FindAsync(1);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal(99m, updated.Price);
    }

    [Fact]
    public async Task Update_SingleEntity_ReturnsUpdatedEntity()
    {
        await SeedAsync(MakeProduct(1, "Original"));

        var product = await DbContext.Products.FindAsync(1);
        product!.Name = "Modified";

        var result = await Repository.Update(product);

        Assert.NotNull(result);
        Assert.Equal("Modified", result.Name);
    }

    [Fact]
    public async Task Update_SingleEntity_SetsUpdatedDate()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Repository.Update(product!);

        Assert.True(product!.UpdatedDate >= before);
        Assert.True(product.UpdatedDate <= DateTime.UtcNow.AddSeconds(1));
    }

    // ── Update bulk ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_MultipleEntities_AllChangesPersistedToDatabase()
    {
        await SeedAsync(MakeProduct(1, "A", 1m), MakeProduct(2, "B", 2m));

        var p1 = await DbContext.Products.FindAsync(1);
        var p2 = await DbContext.Products.FindAsync(2);
        p1!.Price = 100m;
        p2!.Price = 200m;

        await Repository.Update(new[] { p1, p2 }, CancellationToken.None);

        DbContext.ChangeTracker.Clear();
        Assert.Equal(100m, (await DbContext.Products.FindAsync(1))!.Price);
        Assert.Equal(200m, (await DbContext.Products.FindAsync(2))!.Price);
    }

    [Fact]
    public async Task Update_MultipleEntities_SetsUpdatedDateOnAll()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2));
        var products = DbContext.Products.ToList();
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Repository.Update(products.ToArray());

        Assert.All(products, p =>
        {
            Assert.True(p.UpdatedDate >= before);
            Assert.True(p.UpdatedDate <= DateTime.UtcNow.AddSeconds(1));
        });
    }

    // ── Detach ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detach_DetachesEntitiesFromChangeTracker()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);

        // Verify it's being tracked before detach
        Assert.NotEqual(
            Microsoft.EntityFrameworkCore.EntityState.Detached,
            DbContext.Entry(product!).State);

        Repository.Detach([product!]);

        Assert.Equal(
            Microsoft.EntityFrameworkCore.EntityState.Detached,
            DbContext.Entry(product!).State);
    }

    // ── ClearChangeTracker ───────────────────────────────────────────────────

    [Fact]
    public async Task ClearChangeTracker_RemovesAllTrackedEntities()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2));
        // Load both into the tracker
        _ = await DbContext.Products.FindAsync(1);
        _ = await DbContext.Products.FindAsync(2);
        Assert.True(DbContext.ChangeTracker.Entries().Any());

        Repository.ClearChangeTracker();

        Assert.False(DbContext.ChangeTracker.Entries().Any());
    }

    // ── Events ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_PublishesEntityUpdatingEvent()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);

        await Repository.Update(product!);

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<EntityUpdatingEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_PublishesEntityUpdatedEvent()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);

        await Repository.Update(product!);

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<EntityUpdatedEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
