using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="ReadRepository{TEntity,TContext}"/>.
///
/// Covers: GetAll, GetById, GetByIds, FirstOrDefault, GetCount, Where, Query.
/// Filtering (pagination, ordering, property filters) is exercised end-to-end via
/// <see cref="FilterConstraintHandler{TEntity}"/> which is wired into the mock dispatcher
/// in <see cref="RepositoryTestBase{TRepo}"/>.
/// </summary>
public class ReadRepositoryTests : RepositoryTestBase<TestReadRepository>
{
    protected override TestReadRepository CreateRepository(RepositoryDependency dep, TestDbContext ctx)
        => new(dep, ctx);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithNoFilter_ReturnsAllEntities()
    {
        await SeedAsync(
            MakeProduct(1, "Alpha", 10),
            MakeProduct(2, "Beta",  20),
            MakeProduct(3, "Gamma", 30));

        var result = await Repository.GetAll();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAll_WithEmptyDatabase_ReturnsEmpty()
    {
        var result = await Repository.GetAll();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_WithTake_ReturnsCorrectPage()
    {
        await SeedAsync(
            MakeProduct(1), MakeProduct(2), MakeProduct(3),
            MakeProduct(4), MakeProduct(5));

        var filter = new Filter { Skip = 0, Take = 3 };
        var result = await Repository.GetAll(filter);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAll_WithSkipAndTake_ReturnsCorrectWindow()
    {
        await SeedAsync(
            MakeProduct(1, "A"), MakeProduct(2, "B"), MakeProduct(3, "C"),
            MakeProduct(4, "D"), MakeProduct(5, "E"));

        var filter = new Filter { Skip = 2, Take = 2 };
        var result = await Repository.GetAll(filter);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAll_WithOrderByAscending_ReturnsOrderedEntities()
    {
        await SeedAsync(
            MakeProduct(1, price: 30),
            MakeProduct(2, price: 10),
            MakeProduct(3, price: 20));

        var filter = new Filter { OrderBy = ["Price"], Descending = false };
        var result = (await Repository.GetAll(filter)).ToList();

        Assert.Equal(10m, result[0].Price);
        Assert.Equal(20m, result[1].Price);
        Assert.Equal(30m, result[2].Price);
    }

    [Fact]
    public async Task GetAll_WithOrderByDescending_ReturnsReverseOrder()
    {
        await SeedAsync(
            MakeProduct(1, price: 10),
            MakeProduct(2, price: 30),
            MakeProduct(3, price: 20));

        var filter = new Filter { OrderBy = ["Price"], Descending = true };
        var result = (await Repository.GetAll(filter)).ToList();

        Assert.Equal(30m, result[0].Price);
        Assert.Equal(20m, result[1].Price);
        Assert.Equal(10m, result[2].Price);
    }

    [Fact]
    public async Task GetAll_WithPropertyFilter_ReturnsMatchingEntities()
    {
        await SeedAsync(
            MakeProduct(1, price: 5),
            MakeProduct(2, price: 50),
            MakeProduct(3, price: 100));

        var filter = new Filter();
        filter.AddProperty("Price", typeof(decimal), 50m);

        var result = await Repository.GetAll(filter);

        Assert.Single(result);
        Assert.Equal(50m, result.First().Price);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithExistingId_ReturnsEntity()
    {
        await SeedAsync(MakeProduct(42, "Widget"));

        var product = await Repository.GetById(42);

        Assert.NotNull(product);
        Assert.Equal(42, product.Id);
        Assert.Equal("Widget", product.Name);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNull()
    {
        await SeedAsync(MakeProduct(1));

        var product = await Repository.GetById(999);

        Assert.Null(product);
    }

    // ── GetByIds ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIds_WithExistingIds_ReturnsMatchingEntities()
    {
        await SeedAsync(
            MakeProduct(1), MakeProduct(2), MakeProduct(3), MakeProduct(4));

        var result = await Repository.GetByIds([1, 3]);

        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Contains(p.Id, new[] { 1, 3 }));
    }

    [Fact]
    public async Task GetByIds_WithNoMatchingIds_ReturnsEmpty()
    {
        await SeedAsync(MakeProduct(1));

        var result = await Repository.GetByIds([99, 100]);

        Assert.Empty(result);
    }

    // ── FirstOrDefault ────────────────────────────────────────────────────────

    [Fact]
    public async Task FirstOrDefault_WithMatchingFilter_ReturnsFirstEntity()
    {
        await SeedAsync(
            MakeProduct(1, price: 5),
            MakeProduct(2, price: 5),
            MakeProduct(3, price: 99));

        var filter = new Filter();
        filter.AddProperty("Price", typeof(decimal), 5m);

        var result = await Repository.FirstOrDefault(filter);

        Assert.NotNull(result);
        Assert.Equal(5m, result.Price);
    }

    [Fact]
    public async Task FirstOrDefault_WithNoMatch_ReturnsNull()
    {
        await SeedAsync(MakeProduct(1, price: 10));

        var filter = new Filter();
        filter.AddProperty("Price", typeof(decimal), 999m);

        var result = await Repository.FirstOrDefault(filter);

        Assert.Null(result);
    }

    [Fact]
    public async Task FirstOrDefault_WithNullFilter_ReturnsFirstRecord()
    {
        await SeedAsync(MakeProduct(1, "First"), MakeProduct(2, "Second"));

        var result = await Repository.FirstOrDefault();

        Assert.NotNull(result);
    }

    // ── GetCount ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCount_WithNoFilter_ReturnsTotalCount()
    {
        await SeedAsync(MakeProduct(1), MakeProduct(2), MakeProduct(3));

        var count = await Repository.GetCount();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetCount_WithFilter_ReturnsFilteredCount()
    {
        await SeedAsync(
            MakeProduct(1, stock: 5),
            MakeProduct(2, stock: 0),
            MakeProduct(3, stock: 5));

        var filter = new Filter();
        filter.AddProperty("Stock", typeof(int), 5);

        var count = await Repository.GetCount(filter);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetCount_OnEmptyDatabase_ReturnsZero()
    {
        var count = await Repository.GetCount();

        Assert.Equal(0, count);
    }

    // ── Where ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Where_WithPredicate_ReturnsFilteredQueryable()
    {
        await SeedAsync(
            MakeProduct(1, price: 10),
            MakeProduct(2, price: 50),
            MakeProduct(3, price: 100));

        var result = Repository.Where(p => p.Price > 20).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.Price > 20));
    }

    // ── Query property ────────────────────────────────────────────────────────

    [Fact]
    public async Task Query_ReturnsQueryable_AllowsLinq()
    {
        await SeedAsync(
            MakeProduct(1, price: 5),
            MakeProduct(2, price: 15));

        var result = Repository.Query.Where(p => p.Price > 10).ToList();

        Assert.Single(result);
        Assert.Equal(15m, result[0].Price);
    }
}
