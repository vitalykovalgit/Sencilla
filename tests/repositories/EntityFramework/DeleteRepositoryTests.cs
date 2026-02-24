using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="DeleteRepository{TEntity,TContext}"/>.
///
/// Covers: Delete by entity / entities collection (hard physical delete).
///
/// Note: Delete(id) and Delete(IEnumerable&lt;TKey&gt;) use ExecuteDeleteAsync which is a
/// bulk-SQL operation not supported by the EF Core InMemory provider.
/// Those overloads are skipped here and should be tested against a relational provider.
/// </summary>
public class DeleteRepositoryTests : RepositoryTestBase<TestDeleteRepository>
{
    protected override TestDeleteRepository CreateRepository(RepositoryDependency dep, TestDbContext ctx)
        => new(dep, ctx);

    // ── Delete by id (skipped — ExecuteDeleteAsync requires a relational provider) ──

    [Fact(Skip = "Delete(id) uses ExecuteDeleteAsync which is not supported by the EF Core InMemory provider.")]
    public async Task Delete_ById_RemovesEntityFromDatabase()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2));

        await Repository.Delete(1);

        Assert.Null(await DbContext.Products.FindAsync(1));
        Assert.NotNull(await DbContext.Products.FindAsync(2));
    }

    [Fact(Skip = "Delete(id) uses ExecuteDeleteAsync which is not supported by the EF Core InMemory provider.")]
    public async Task Delete_ById_ReturnsDeletedCount()
    {
        await SeedAsync(MakeProduct(1));

        var count = await Repository.Delete(1);

        Assert.Equal(1, count);
    }

    [Fact(Skip = "Delete(id) uses ExecuteDeleteAsync which is not supported by the EF Core InMemory provider.")]
    public async Task Delete_ByNonExistingId_ReturnsZero()
    {
        await SeedAsync(MakeProduct(1));

        var count = await Repository.Delete(999);

        Assert.Equal(0, count);
    }

    // ── Delete by ids (skipped — ExecuteDeleteAsync requires a relational provider) ─

    [Fact(Skip = "Delete(IEnumerable<TKey>) uses ExecuteDeleteAsync which is not supported by the EF Core InMemory provider.")]
    public async Task Delete_ByIds_RemovesAllMatchingEntities()
    {
        await SeedAsync(
            MakeProduct(1), MakeProduct(2), MakeProduct(3), MakeProduct(4));

        await Repository.Delete(new[] { 1, 3 });

        Assert.Null(await DbContext.Products.FindAsync(1));
        Assert.NotNull(await DbContext.Products.FindAsync(2));
        Assert.Null(await DbContext.Products.FindAsync(3));
        Assert.NotNull(await DbContext.Products.FindAsync(4));
    }

    [Fact(Skip = "Delete(IEnumerable<TKey>) uses ExecuteDeleteAsync which is not supported by the EF Core InMemory provider.")]
    public async Task Delete_ByIds_ReturnsCountOfDeletedEntities()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2), MakeProduct(3));

        var count = await Repository.Delete(new[] { 1, 2 });

        Assert.Equal(2, count);
    }

    [Fact(Skip = "Delete(IEnumerable<TKey>) uses ExecuteDeleteAsync which is not supported by the EF Core InMemory provider.")]
    public async Task Delete_ByIds_WithNoMatches_ReturnsZero()
    {
        await SeedAsync(MakeProduct(1));

        var count = await Repository.Delete(new[] { 99, 100 });

        Assert.Equal(0, count);
    }

    // ── Delete by entity ─────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ByEntity_RemovesEntityFromDatabase()
    {
        await SeedAsync(MakeProduct(1, "ToDelete"), MakeProduct(2, "KeepMe"));
        var product = await DbContext.Products.FindAsync(1);

        await Repository.Delete(product!);

        DbContext.ChangeTracker.Clear();
        Assert.Null(await DbContext.Products.FindAsync(1));
        Assert.NotNull(await DbContext.Products.FindAsync(2));
    }

    [Fact]
    public async Task Delete_ByEntity_ReturnsOne()
    {
        await SeedAsync(MakeProduct(1));
        var product = await DbContext.Products.FindAsync(1);

        var count = await Repository.Delete(product!);

        Assert.Equal(1, count);
    }

    // ── Delete by entities collection ────────────────────────────────────────

    [Fact]
    public async Task Delete_ByEntities_RemovesAllFromDatabase()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2), MakeProduct(3));
        var toDelete = DbContext.Products.Where(p => p.Id <= 2).ToList();

        await Repository.Delete(toDelete);

        DbContext.ChangeTracker.Clear();
        Assert.Null(await DbContext.Products.FindAsync(1));
        Assert.Null(await DbContext.Products.FindAsync(2));
        Assert.NotNull(await DbContext.Products.FindAsync(3));
    }

    [Fact]
    public async Task Delete_ByEntities_ReturnsCountOfDeletedEntities()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2), MakeProduct(3));
        var toDelete = DbContext.Products.ToList();

        var count = await Repository.Delete(toDelete);

        Assert.Equal(3, count);
    }
}
