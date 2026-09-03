using Microsoft.Data.Sqlite;
using Sencilla.Repository.EntityFramework.Tests.Infrastructure;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Verifies the explicit transaction seam: one Begin groups writes made through several
/// repositories, and a nested Begin defers to the outermost owner. Sqlite in-memory because
/// the InMemory provider has no transactions (that path is asserted separately).
/// </summary>
public class TransactionFactoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _db;

    public TransactionFactoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _db = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
    }

    private RepositoryDependency Dependency => new(
        Mock.Of<IServiceProvider>(), Mock.Of<IEventDispatcher>(), Mock.Of<ICommandDispatcher>());

    private ITransactionFactory Factory => new EfTransactionFactory<TestDbContext>(_db);

    private int CountInDb() => _db.Products.AsNoTracking().Count();

    [Fact]
    public async Task Begin_CommitsEveryRepositoryWrite_AsOneUnit()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        using (var tx = await Factory.Begin())
        {
            await repo.Create(new TestProduct { Name = "first" });
            await repo.Create(new TestProduct { Name = "second" });
            await tx.CommitAsync();
        }

        Assert.Equal(2, CountInDb());
    }

    [Fact]
    public async Task Begin_RollsBackEveryRepositoryWrite_WhenDisposedWithoutCommit()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        using (var tx = await Factory.Begin())
        {
            await repo.Create(new TestProduct { Name = "first" });
            await repo.Create(new TestProduct { Name = "second" });
            // no CommitAsync — leaving the block must discard both.
        }

        Assert.Equal(0, CountInDb());
    }

    [Fact]
    public async Task Begin_JoinsTheOpenTransaction_InsteadOfNesting()
    {
        using var outer = await Factory.Begin();
        var opened = _db.Database.CurrentTransaction;

        using var inner = await Factory.Begin();

        // Same transaction, and the inner handle is the inert one — committing it must not
        // end the outer unit.
        Assert.Same(opened, _db.Database.CurrentTransaction);
        await inner.CommitAsync();
        Assert.NotNull(_db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Begin_JoinsATransactionOpenedByARepository()
    {
        // The old seam and the new one must interoperate during the migration.
        using var repoTx = await new TestReadRepository(Dependency, _db).BeginTransaction();
        var opened = _db.Database.CurrentTransaction;

        using var tx = await Factory.Begin();

        Assert.Same(opened, _db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task InnerRollback_DiscardsTheWholeUnit()
    {
        var repo = new TestCreateRepository(Dependency, _db);

        using (var outer = await Factory.Begin())
        {
            await repo.Create(new TestProduct { Name = "doomed" });

            using (var inner = await Factory.Begin())
                await inner.RollbackAsync();

            // The outer owner cannot commit a unit an inner scope threw away.
            await Assert.ThrowsAnyAsync<Exception>(() => outer.CommitAsync());
        }

        Assert.Equal(0, CountInDb());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
