using Microsoft.Data.SqlClient;
using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Upsert, merge and get-or-create on a real SQL Server, the only provider that runs their MERGEs. What is proved is
/// that the permission events judge the rows the MERGE really wrote. A database-generated key exists nowhere but in the
/// MERGE's OUTPUT: the Created post-image used to look the rows up by the body's empty key, find none, and pass — and
/// get-or-create handed back whatever it matched without asking Read.
/// </summary>
public class RawMergeEnforcementTests : IAsyncLifetime
{
    private readonly Mock<IEventDispatcher> Events = new();
    private readonly TestDbContext Db;
    private readonly TestCreateRepository Repository;

    public RawMergeEnforcementTests()
    {
        var database = new SqlConnectionStringBuilder(SqlServerFactAttribute.ConnectionString) { InitialCatalog = $"sencilla_merge_{Guid.NewGuid():N}" };
        Db = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(database.ConnectionString).Options);
        Repository = new TestCreateRepository(new RepositoryDependency(Mock.Of<IServiceProvider>(), Events.Object, Mock.Of<ICommandDispatcher>()), Db);
    }

    public Task InitializeAsync() => Db.Database.EnsureCreatedAsync();

    public async Task DisposeAsync()
    {
        await Db.Database.EnsureDeletedAsync();
        await Db.DisposeAsync();
    }

    // ── The rows a MERGE inserts are the rows the Create check judges ────────

    [SqlServerFact]
    public async Task Upsert_JudgesTheRowItInserted_NotTheBodysEmptyKey()
    {
        DenyCreated();

        await Assert.ThrowsAsync<ForbiddenException>(() => Repository.UpsertAsync(new TestProduct { Name = "new" }, x => x.Id));

        Assert.Empty(await Rows());
    }

    [SqlServerFact]
    public async Task Merge_JudgesTheRowItInserted_NotTheBodysEmptyKey()
    {
        DenyCreated();

        await Assert.ThrowsAsync<ForbiddenException>(() => Repository.MergeAsync(new TestProduct { Name = "new" }, x => x.Id));

        Assert.Empty(await Rows());
    }

    [SqlServerFact]
    public async Task GetOrCreate_JudgesTheRowItInserted_NotTheBodysEmptyKey()
    {
        DenyCreated();

        await Assert.ThrowsAsync<ForbiddenException>(() => Repository.GetOrCreateAsync([new TestProduct { Name = "new" }]));

        Assert.Empty(await Rows());
    }

    /// The Created event carries the inserted rows with the keys the database gave them — the self-securing check and
    /// role provisioning read those ids — and nothing the MERGE updated or deleted.
    [SqlServerFact]
    public async Task Merge_PublishesOnlyTheRowItInserted_WithItsKey()
    {
        await Seed(new TestProduct { Name = "kept" }, new TestProduct { Name = "dropped" });

        await Repository.MergeAsync(new[] { new TestProduct { Name = "kept", Stock = 1 }, new TestProduct { Name = "new" } }, x => x.Name, null, null);

        var rows = await Rows();
        Assert.Equal(["kept", "new"], rows.Select(p => p.Name).Order());
        var created = Assert.Single(Published<EntityCreatedEvent<TestProduct>>()).Entities!.ToList();
        Assert.Equal(rows.Single(p => p.Name == "new").Id, Assert.Single(created).Id);
    }

    // ── get-or-create answers like a read ────────────────────────────────────

    [SqlServerFact]
    public async Task GetOrCreate_ReturnsTheRowItInserted_WithItsKey()
    {
        var result = await Repository.GetOrCreateAsync([new TestProduct { Name = "new" }]);

        Assert.Equal(Assert.Single(await Rows()).Id, Assert.Single(result.Created).Id);
        Assert.Empty(result.Existing);
    }

    [SqlServerFact]
    public async Task GetOrCreate_LeavesOutAMatchedRowTheCallerMayNotRead()
    {
        await Seed(new TestProduct { Name = "theirs" });
        Events.Setup(x => x.PublishAsync(It.IsAny<EntityReadingEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
              .Returns<EntityReadingEvent<TestProduct>, CancellationToken>((e, _) =>
              {
                  e.Entities = e.Entities!.Where(p => p.Name != "theirs");
                  return Task.CompletedTask;
              });

        var result = await Repository.GetOrCreateAsync([new TestProduct { Name = "theirs" }, new TestProduct { Name = "mine" }], nameof(TestProduct.Name));

        Assert.Equal("mine", Assert.Single(result.All).Name);
        Assert.Equal(2, (await Rows()).Count);
    }

    /// The MERGE text reaches the server as it is. It once went through string.Format, which choked on these braces.
    [SqlServerFact]
    public async Task Upsert_KeepsTheBracesOfAJsonValue()
    {
        const string json = """{"d":"M 0 0 H 1 V 1 H 0 Z","defaultColor":"#B71C1C"}""";

        await Repository.UpsertAsync(new TestProduct { Name = json }, x => x.Id);

        Assert.Equal(json, Assert.Single(await Rows()).Name);
    }

    /// <summary>The Created post-image, narrowed the way a constraint that admits none of the rows would.</summary>
    private void DenyCreated() =>
        Events.Setup(x => x.PublishAsync(It.IsAny<EntityCreatedEvent<TestProduct>>(), It.IsAny<CancellationToken>()))
              .Returns<EntityCreatedEvent<TestProduct>, CancellationToken>((e, _) =>
              {
                  e.DbEntities = e.DbEntities!.Where(p => false);
                  return Task.CompletedTask;
              });

    private List<TEvent> Published<TEvent>() => Events.Invocations.Select(i => i.Arguments[0]).OfType<TEvent>().ToList();

    private Task<List<TestProduct>> Rows() => Db.Products.AsNoTracking().ToListAsync();

    private async Task Seed(params TestProduct[] products)
    {
        Db.Products.AddRange(products);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }
}
