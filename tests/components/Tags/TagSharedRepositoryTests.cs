namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// The shared table reuses every line of <see cref="TagLinkedRepository{TEntity,TKey,TRow}"/> and overrides only
/// how a row addresses its parent, so these tests cover exactly that override: the type discriminator scopes
/// every read, the key round-trips through text, and an unparseable id is skipped instead of breaking a read.
/// </summary>
public class TagSharedRepositoryTests : IDisposable
{
    readonly TagTestHost _host = new();
    ITagRepository<SharedThing, int> Tags => _host.Tags<SharedThing, int>();

    [Fact]
    public async Task Set_WritesDiscriminatedRows()
    {
        await _host.SeedAsync(new SharedThing { Id = 1 });

        await Tags.Set(1, ["Alpha", "beta"]);

        var rows = await _host.Context.Set<EntityTag>().AsNoTracking().ToListAsync();
        Assert.All(rows, r => Assert.Equal(nameof(SharedThing), r.Entity));
        Assert.All(rows, r => Assert.Equal("1", r.EntityId));
        Assert.Equal(["alpha", "beta"], rows.Select(r => r.Name).Order());
    }

    [Fact]
    public async Task Reads_IgnoreOtherEntityTypes()
    {
        // The same id under a different entity name must be invisible — the discriminator is the only thing
        // separating two entities' tags in one table.
        await _host.SeedAsync(new SharedThing { Id = 1 });
        await _host.SeedAsync(
            new EntityTag { Entity = nameof(SharedThing), EntityId = "1", Name = "mine" },
            new EntityTag { Entity = "SomethingElse", EntityId = "1", Name = "theirs" });

        Assert.Equal(["mine"], await Tags.Get(1));
        Assert.Equal(["mine"], await Tags.Distinct());

        var matched = await Tags.FilterAny(_host.Context.Set<SharedThing>(), ["theirs"]);
        Assert.Empty(await matched.ToListAsync());
    }

    [Fact]
    public async Task Set_IsDiffed_AndTouchesTheParentOnlyWhenSomethingChanges()
    {
        await _host.SeedAsync(new SharedThing { Id = 1 });
        await Tags.Set(1, ["alpha"]);
        _host.Touched.Clear();

        await Tags.Set(1, ["ALPHA"]);   // same set after normalisation

        Assert.Empty(_host.Touched);
        Assert.Single(await _host.Context.Set<EntityTag>().ToListAsync());
    }

    [Fact]
    public async Task Hydrate_FillsTheWireFieldFromTheSharedTable()
    {
        await _host.SeedAsync(new SharedThing { Id = 1 }, new SharedThing { Id = 2 });
        await _host.SeedAsync(
            new EntityTag { Entity = nameof(SharedThing), EntityId = "1", Name = "beta" },
            new EntityTag { Entity = nameof(SharedThing), EntityId = "1", Name = "alpha" },
            new EntityTag { Entity = nameof(SharedThing), EntityId = "2", Name = "gamma" });

        var page = new List<SharedThing> { new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 } };
        await Tags.Hydrate(page);

        Assert.Equal(["alpha", "beta"], page[0].Tags);
        Assert.Equal(["gamma"], page[1].Tags);
        Assert.Null(page[2].Tags);
    }

    [Fact]
    public async Task FilterAny_NarrowsToTheMatchingParents()
    {
        await _host.SeedAsync(new SharedThing { Id = 1 }, new SharedThing { Id = 2 }, new SharedThing { Id = 3 });
        await _host.SeedAsync(
            new EntityTag { Entity = nameof(SharedThing), EntityId = "1", Name = "alpha" },
            new EntityTag { Entity = nameof(SharedThing), EntityId = "3", Name = "beta" });

        var matched = await Tags.FilterAny(_host.Context.Set<SharedThing>(), ["alpha", "beta"]);

        Assert.Equal([1, 3], (await matched.Select(e => e.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task UnparseableId_IsSkippedRatherThanFatal()
    {
        // The price of a string key with no foreign key: a stale or hand-edited row cannot be allowed to throw
        // in the middle of someone else's read.
        await _host.SeedAsync(new SharedThing { Id = 1 });
        await _host.SeedAsync(
            new EntityTag { Entity = nameof(SharedThing), EntityId = "not-a-number", Name = "junk" },
            new EntityTag { Entity = nameof(SharedThing), EntityId = "1", Name = "good" });

        var page = new List<SharedThing> { new() { Id = 1 } };
        await Tags.Hydrate(page);
        Assert.Equal(["good"], page[0].Tags);

        var matched = await Tags.FilterAny(_host.Context.Set<SharedThing>(), ["junk", "good"]);
        Assert.Equal([1], await matched.Select(e => e.Id).ToListAsync());
    }

    [Fact]
    public async Task Clear_DropsOnlyThisEntityTypesRows()
    {
        await _host.SeedAsync(new SharedThing { Id = 1 });
        await _host.SeedAsync(
            new EntityTag { Entity = nameof(SharedThing), EntityId = "1", Name = "mine" },
            new EntityTag { Entity = "SomethingElse", EntityId = "1", Name = "theirs" });

        await _host.Get<TagSharedRepository<SharedThing, int>>().Clear([1]);

        var left = await _host.Context.Set<EntityTag>().AsNoTracking().ToListAsync();
        Assert.Equal(["theirs"], left.Select(r => r.Name));
    }

    public void Dispose() => _host.Dispose();
}
