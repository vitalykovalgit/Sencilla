namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// The row-per-tag repository against a real database, which is the only way to prove what it actually relies
/// on: that the FK read and written generically through EF's model (<c>EF.Property</c>) translates, that the
/// projections and the DISTINCT push-down translate, and that the bulk delete does.
///
/// <para>Behaviour that matters beyond translation: writes are DIFFED (re-saving an unchanged set produces no
/// audit noise on the parent) and they touch the parent when they do change.</para>
/// </summary>
public class TagLinkedRepositoryTests : IDisposable
{
    static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    readonly TagTestHost _host = new();
    ITagRepository<LinkedThing, Guid> Tags => _host.Tags<LinkedThing, Guid>();

    [Fact]
    public async Task Set_InsertsOnlyNewTagsAndDeletesOnlyDroppedOnes()
    {
        await SeedAsync(Row("alpha"), Row("beta"));

        var before = await _host.Context.Set<LinkedThingTag>().AsNoTracking().SingleAsync(r => r.Name == "beta");

        await Tags.Set(Id, ["beta", "gamma"]);

        Assert.Equal(["beta", "gamma"], await Tags.Get(Id));

        // The surviving row is the ORIGINAL one — a blanket delete-and-reinsert would have replaced it.
        var after = await _host.Context.Set<LinkedThingTag>().AsNoTracking().SingleAsync(r => r.Name == "beta");
        Assert.Equal(before.Id, after.Id);
        Assert.Single(_host.Touched);
    }

    [Fact]
    public async Task Set_UnchangedSet_WritesNothingAtAll()
    {
        await SeedAsync(Row("alpha"), Row("beta"));

        await Tags.Set(Id, ["Beta", "ALPHA"]);   // same set after normalisation

        Assert.Equal(2, await _host.Context.Set<LinkedThingTag>().CountAsync());
        Assert.Empty(_host.Touched);
    }

    [Fact]
    public async Task Set_Empty_ClearsEveryRow()
    {
        await SeedAsync(Row("alpha"), Row("beta"));

        await Tags.Set(Id, []);

        Assert.Empty(await Tags.Get(Id));
        Assert.Empty(await _host.Context.Set<LinkedThingTag>().ToListAsync());
    }

    [Fact]
    public async Task Set_OnlyTouchesTheRowsOfThatParent()
    {
        await SeedAsync(Row("alpha"), Row("delta", Other));

        await Tags.Set(Id, ["gamma"]);

        Assert.Equal(["gamma"], await Tags.Get(Id));
        Assert.Equal(["delta"], await Tags.Get(Other));
    }

    [Fact]
    public async Task Get_IsDedupedAndOrdinallySorted()
    {
        await SeedAsync(Row("gamma"), Row("alpha"));

        Assert.Equal(["alpha", "gamma"], await Tags.Get(Id));
    }

    [Fact]
    public async Task Hydrate_FillsTheWireFieldForTheWholePage_LeavingUntaggedRowsNull()
    {
        await SeedAsync(Row("beta"), Row("alpha"), Row("delta", Other));
        var page = new List<LinkedThing> { new() { Id = Id }, new() { Id = Other }, new() { Id = Guid.NewGuid() } };

        await Tags.Hydrate(page);

        Assert.Equal(["alpha", "beta"], page[0].Tags);
        Assert.Equal(["delta"], page[1].Tags);
        Assert.Null(page[2].Tags);
    }

    [Fact]
    public async Task Hydrate_EmptyPage_IsANoOp()
    {
        await Tags.Hydrate([]);
    }

    [Fact]
    public async Task FilterAny_NarrowsToTheMatchingParents()
    {
        await SeedAsync(Row("alpha"), Row("delta", Other));
        await _host.SeedAsync(new LinkedThing { Id = Guid.NewGuid() });   // untagged parent, must not match

        var matched = await Tags.FilterAny(_host.Context.Set<LinkedThing>(), ["alpha", "delta"]);

        Assert.Equal([Id, Other], (await matched.Select(e => e.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task FilterAny_NoMatchingRows_MatchesNothing()
    {
        await SeedAsync();

        var matched = await Tags.FilterAny(_host.Context.Set<LinkedThing>(), ["alpha"]);

        Assert.Empty(await matched.ToListAsync());
    }

    [Fact]
    public async Task Distinct_IsDedupedAndOrdinallySorted()
    {
        await SeedAsync(Row("gamma"), Row("alpha"), Row("alpha", Other));

        Assert.Equal(["alpha", "gamma"], await Tags.Distinct());
    }

    [Fact]
    public async Task Clear_DropsRowsWithoutTouchingTheParent()
    {
        // Used when the parents themselves are being deleted — a touch would be noise on a doomed row.
        await SeedAsync(Row("alpha"), Row("delta", Other));

        // Clear is not on ITagRepository — it exists for the delete-sweep handler, which holds the concrete type.
        await _host.Get<TagLinkedRepository<LinkedThing, Guid, LinkedThingTag>>().Clear([Id]);

        Assert.Empty(await Tags.Get(Id));
        Assert.Equal(["delta"], await Tags.Get(Other));
        Assert.Empty(_host.Touched);
    }

    static LinkedThingTag Row(string name, Guid? entityId = null)
        => new() { EntityId = entityId ?? Id, Name = name };

    /// <summary>Parents first — the link table's FK is real.</summary>
    async Task SeedAsync(params LinkedThingTag[] rows)
    {
        await _host.SeedAsync(new LinkedThing { Id = Id }, new LinkedThing { Id = Other });
        await _host.SeedAsync([.. rows]);
    }

    public void Dispose() => _host.Dispose();
}
