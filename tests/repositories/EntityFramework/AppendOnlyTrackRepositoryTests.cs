using Sencilla.Repository.EntityFramework.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Tests for <see cref="AppendOnlyTrackRepository{TEntity,TContext,TKey}"/> and
/// <see cref="AppendOnlyInterceptor"/>: supersede (immediate + scheduled), append-to-the-end validation,
/// LIFO cancel of a scheduled version, and the append-only mutation guard.
///
/// Uses the in-memory provider with the interceptor wired and transactions ignored, so it exercises the
/// supersede/cancel/guard <i>logic</i>. The open-row unique index and real transactional isolation are a
/// SQL-Server concern and are not covered here.
/// </summary>
public class AppendOnlyTrackRepositoryTests : IDisposable
{
    const int From = 1;
    const int To = 2;

    readonly TestDbContext _db;
    readonly AppendOnlyTrackRepository<TestRate, TestDbContext, int> _repo;

    public AppendOnlyTrackRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(AppendOnlyInterceptor.Instance)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new TestDbContext(options);

        var dispatcher = new Mock<IEventDispatcher>();
        dispatcher.Setup(x => x.PublishAsync(It.IsAny<EntityCreatingEvent<TestRate>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        dispatcher.Setup(x => x.PublishAsync(It.IsAny<EntityCreatedEvent<TestRate>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var dependency = new RepositoryDependency(
            new Mock<IServiceProvider>().Object, dispatcher.Object, new Mock<ICommandDispatcher>().Object);

        _repo = new AppendOnlyTrackRepository<TestRate, TestDbContext, int>(dependency, _db);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Seed an open (active-since-the-past) version directly, bypassing the repository.</summary>
    async Task SeedActive(decimal rate)
    {
        _db.Rates.Add(new TestRate { From = From, To = To, Rate = rate, ActiveFrom = DateTime.UtcNow.AddDays(-10), ActiveTo = null });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    static TestRate Version(decimal rate, DateTime? activeFrom = null)
        => new() { From = From, To = To, Rate = rate, ActiveFrom = activeFrom };

    static TestRate Key() => new() { From = From, To = To };

    List<TestRate> AllVersions() => _db.Rates.AsNoTracking().Where(r => r.From == From && r.To == To).ToList();

    decimal? RateAsOf(DateTime at) => _db.Rates.AsNoTracking()
        .ActiveAsOf(at).Where(r => r.From == From && r.To == To)
        .Select(r => (decimal?)r.Rate).SingleOrDefault();

    // ── supersede: immediate ───────────────────────────────────────────────────

    [Fact]
    public async Task Supersede_Immediate_ClosesPriorAndOpensNew()
    {
        await SeedActive(10m);

        await _repo.Create(Version(20m));

        var versions = AllVersions();
        Assert.Equal(2, versions.Count);

        var prior = versions.Single(v => v.Rate == 10m);
        var current = versions.Single(v => v.Rate == 20m);
        Assert.NotNull(prior.ActiveTo);                      // closed
        Assert.Null(current.ActiveTo);                       // open
        Assert.Equal(prior.ActiveTo, current.ActiveFrom);    // contiguous
        Assert.Equal(20m, RateAsOf(DateTime.UtcNow));        // now resolves to the new rate
    }

    [Fact]
    public async Task Supersede_PastActiveFrom_SnapsToNow()
    {
        await SeedActive(10m);
        var before = DateTime.UtcNow;

        await _repo.Create(Version(20m, DateTime.UtcNow.AddDays(-3)));   // a past date must not backdate

        var current = AllVersions().Single(v => v.Rate == 20m);
        Assert.True(current.ActiveFrom >= before);
        Assert.Equal(20m, RateAsOf(DateTime.UtcNow));
    }

    // ── supersede: scheduled ────────────────────────────────────────────────────

    [Fact]
    public async Task Supersede_Future_SchedulesWithoutActivating()
    {
        await SeedActive(10m);
        var when = DateTime.UtcNow.AddDays(5);

        await _repo.Create(Version(20m, when));

        var prior = AllVersions().Single(v => v.Rate == 10m);
        var scheduled = AllVersions().Single(v => v.Rate == 20m);
        Assert.Equal(when, prior.ActiveTo);
        Assert.Equal(when, scheduled.ActiveFrom);
        Assert.Null(scheduled.ActiveTo);

        Assert.Equal(10m, RateAsOf(DateTime.UtcNow));        // still the old rate now
        Assert.Equal(20m, RateAsOf(when.AddHours(1)));       // the new rate after the cutover
    }

    [Fact]
    public async Task Supersede_StacksMultipleScheduledInOrder()
    {
        await SeedActive(10m);
        var d5 = DateTime.UtcNow.AddDays(5);
        var d10 = DateTime.UtcNow.AddDays(10);

        await _repo.Create(Version(20m, d5));
        await _repo.Create(Version(30m, d10));

        Assert.Equal(3, AllVersions().Count);
        Assert.Equal(10m, RateAsOf(DateTime.UtcNow));
        Assert.Equal(20m, RateAsOf(d5.AddHours(1)));
        Assert.Equal(30m, RateAsOf(d10.AddHours(1)));
    }

    [Fact]
    public async Task Supersede_OutOfOrderSchedule_Throws()
    {
        await SeedActive(10m);
        await _repo.Create(Version(30m, DateTime.UtcNow.AddDays(10)));

        await Assert.ThrowsAsync<BadRequestException>(
            () => _repo.Create(Version(25m, DateTime.UtcNow.AddDays(5))));
    }

    [Fact]
    public async Task Supersede_ImmediateWhileScheduledPending_Throws()
    {
        await SeedActive(10m);
        await _repo.Create(Version(20m, DateTime.UtcNow.AddDays(5)));

        await Assert.ThrowsAsync<BadRequestException>(() => _repo.Create(Version(25m)));
    }

    // ── cancel (LIFO) ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelPending_RemovesTail_ReopensPrior()
    {
        await SeedActive(10m);
        await _repo.Create(Version(20m, DateTime.UtcNow.AddDays(5)));
        Assert.Equal(2, AllVersions().Count);

        await _repo.CancelPending(Key());

        var only = Assert.Single(AllVersions());
        Assert.Equal(10m, only.Rate);
        Assert.Null(only.ActiveTo);                          // reopened
    }

    [Fact]
    public async Task CancelPending_NoPending_Throws()
    {
        await SeedActive(10m);

        await Assert.ThrowsAsync<BadRequestException>(() => _repo.CancelPending(Key()));
    }

    [Fact]
    public async Task CancelPending_Multiple_PopsLastOnly()
    {
        await SeedActive(10m);
        var d5 = DateTime.UtcNow.AddDays(5);
        var d10 = DateTime.UtcNow.AddDays(10);
        await _repo.Create(Version(20m, d5));
        await _repo.Create(Version(30m, d10));

        await _repo.CancelPending(Key());                    // pops 30, reopens 20

        Assert.Equal(2, AllVersions().Count);
        Assert.DoesNotContain(AllVersions(), v => v.Rate == 30m);
        Assert.Equal(20m, RateAsOf(d5.AddHours(1)));
        Assert.Equal(20m, RateAsOf(d10.AddHours(1)));
    }

    // ── interceptor: append-only guard ──────────────────────────────────────────

    [Fact]
    public async Task Interceptor_RejectsContentEditOfActiveVersion()
    {
        await SeedActive(10m);
        var row = _db.Rates.Single();
        row.Rate = 99m;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task Interceptor_RejectsDeleteOfActiveVersion()
    {
        await SeedActive(10m);
        _db.Rates.Remove(_db.Rates.Single());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task Interceptor_AllowsEditOfScheduledVersion()
    {
        await SeedScheduled(20m, DateTime.UtcNow.AddDays(5));

        var row = _db.Rates.Single();
        row.Rate = 25m;
        await _db.SaveChangesAsync();                        // must not throw

        Assert.Equal(25m, _db.Rates.AsNoTracking().Single().Rate);
    }

    [Fact]
    public async Task Interceptor_AllowsDeleteOfScheduledVersion()
    {
        await SeedScheduled(20m, DateTime.UtcNow.AddDays(5));

        _db.Rates.Remove(_db.Rates.Single());
        await _db.SaveChangesAsync();                        // must not throw

        Assert.Empty(_db.Rates.AsNoTracking());
    }

    async Task SeedScheduled(decimal rate, DateTime activeFrom)
    {
        _db.Rates.Add(new TestRate { From = From, To = To, Rate = rate, ActiveFrom = activeFrom, ActiveTo = null });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }
}
