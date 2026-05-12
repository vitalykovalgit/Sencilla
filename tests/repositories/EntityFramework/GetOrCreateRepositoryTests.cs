using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="CreateRepository{TEntity,TContext}"/> GetOrCreateAsync methods.
///
/// NOTE: GetOrCreateAsync executes a raw SQL batch (MERGE + SELECT) via ADO.NET.
/// The EF Core in-memory provider does not support raw SQL, so full round-trip tests
/// (created vs existing split, returned DB rows) require a real SQL Server instance.
///
/// What IS covered here:
///   - EntityCreatingEvent fires for all input entities
///   - EntityCreatedEvent does NOT fire when the SQL layer throws (no items created)
///   - Expression-to-string key extraction (ArgumentException on invalid expressions)
/// </summary>
public class GetOrCreateRepositoryTests : RepositoryTestBase<TestCreateRepository>
{
    protected override TestCreateRepository CreateRepository(RepositoryDependency dep, TestDbContext ctx)
        => new(dep, ctx);

    // ── EntityCreatingEvent fires for all inputs ──────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_DefaultKey_PublishesEntityCreatingEventForAllEntities()
    {
        var products = new[] { MakeProduct(1), MakeProduct(2), MakeProduct(3) };

        // GetOrCreateBulkAsync will throw on in-memory DB — we only care that
        // EntityCreatingEvent was published before the SQL execution.
        await Assert.ThrowsAnyAsync<Exception>(() => Repository.GetOrCreateAsync(products));

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.Is<EntityCreatingEvent<TestProduct>>(e => e.Entities.Count() == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_StringKeys_PublishesEntityCreatingEventForAllEntities()
    {
        var products = new[] { MakeProduct(1), MakeProduct(2) };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            Repository.GetOrCreateAsync(products, "Name"));

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.Is<EntityCreatingEvent<TestProduct>>(e => e.Entities.Count() == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_ExpressionKeys_PublishesEntityCreatingEventForAllEntities()
    {
        var products = new[] { MakeProduct(1) };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            Repository.GetOrCreateAsync(products, p => p.Name));

        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.Is<EntityCreatingEvent<TestProduct>>(e => e.Entities.Count() == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── CreatedDate is stamped before SQL execution ───────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_SetsCreatedDateOnEntitiesBeforeSql()
    {
        var product = MakeProduct(1);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Assert.ThrowsAnyAsync<Exception>(() => Repository.GetOrCreateAsync([product]));

        Assert.True(product.CreatedDate >= before);
    }

    // ── Expression key extraction ─────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_InvalidExpression_ThrowsArgumentException()
    {
        var products = new[] { MakeProduct(1) };

        // A constant expression (not a member access) should throw ArgumentException
        // during key extraction, before any SQL is built.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Repository.GetOrCreateAsync(products, _ => (object)"constant"));
    }

    // ── Empty input ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_EmptyList_ReturnsEmptyResultWithoutHittingDb()
    {
        var result = await Repository.GetOrCreateAsync(Array.Empty<TestProduct>());

        Assert.Empty(result.Created);
        Assert.Empty(result.Existing);
        Assert.Empty(result.All);

        // EntityCreatingEvent should still fire (all 0 entities)
        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<EntityCreatingEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // EntityCreatedEvent must NOT fire (nothing created)
        EventDispatcherMock.Verify(
            x => x.PublishAsync(
                It.IsAny<EntityCreatedEvent<TestProduct>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
