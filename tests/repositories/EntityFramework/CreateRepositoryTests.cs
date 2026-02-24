using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="CreateRepository{TEntity,TContext}"/>.
///
/// Covers: Create single, Create bulk, CreatedDate auto-set,
/// EntityCreatingEvent and EntityCreatedEvent published.
/// </summary>
public class CreateRepositoryTests : RepositoryTestBase<TestCreateRepository>
{
    protected override TestCreateRepository CreateRepository(RepositoryDependency dep, TestDbContext ctx)
        => new(dep, ctx);

    // ── Create single ────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_SingleEntity_PersistsToDatabase()
    {
        var product = MakeProduct(1, "Widget", 9.99m);

        await Repository.Create(product);

        var saved = await DbContext.Products.FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal("Widget", saved.Name);
    }

    [Fact]
    public async Task Create_SingleEntity_ReturnsCreatedEntity()
    {
        var product = MakeProduct(1, "Gadget", 19.99m);

        var result = await Repository.Create(product);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Gadget", result.Name);
    }

    [Fact]
    public async Task Create_SingleEntity_SetsCreatedDate()
    {
        var product = MakeProduct(1);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Repository.Create(product);

        Assert.True(product.CreatedDate >= before);
        Assert.True(product.CreatedDate <= DateTime.UtcNow.AddSeconds(1));
    }

    // ── Create bulk ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_MultipleEntities_AllPersistedToDatabase()
    {
        var products = new[]
        {
            MakeProduct(1, "Alpha"),
            MakeProduct(2, "Beta"),
            MakeProduct(3, "Gamma"),
        };

        await Repository.Create(products);

        Assert.Equal(3, DbContext.Products.Count());
    }

    [Fact]
    public async Task Create_MultipleEntities_ReturnsAllEntities()
    {
        var products = new[]
        {
            MakeProduct(1, "X"),
            MakeProduct(2, "Y"),
        };

        var result = await Repository.Create(products, CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Create_MultipleEntities_SetsCreatedDateOnAll()
    {
        var products = new[]
        {
            MakeProduct(1),
            MakeProduct(2),
            MakeProduct(3),
        };
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Repository.Create(products);

        Assert.All(products, p =>
        {
            Assert.True(p.CreatedDate >= before);
            Assert.True(p.CreatedDate <= DateTime.UtcNow.AddSeconds(1));
        });
    }

    // ── Events ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_PublishesEntityCreatingEvent()
    {
        var product = MakeProduct(1);

        await Repository.Create(product);

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<EntityCreatingEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_PublishesEntityCreatedEvent()
    {
        var product = MakeProduct(1);

        await Repository.Create(product);

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<EntityCreatedEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
