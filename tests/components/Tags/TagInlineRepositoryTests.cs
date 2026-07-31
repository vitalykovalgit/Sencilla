namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// Tags in a JSON-array column on the entity's own row. Its write IS an update of the tagged row, which is what
/// makes a tag edit visible to the audit log and to row-keyed caches.
/// </summary>
public class TagInlineRepositoryTests : IDisposable
{
    readonly TagTestHost _host = new();
    ITagRepository<InlineThing, int> Tags => _host.Tags<InlineThing, int>();

    [Fact]
    public async Task Set_NormalisesBeforeStoring()
    {
        await _host.SeedAsync(new InlineThing { Id = 1 });

        await Tags.Set(1, ["Gamma", "alpha", "ALPHA", " beta "]);

        Assert.Equal(["alpha", "beta", "gamma"], await Tags.Get(1));
    }

    [Fact]
    public async Task Set_Empty_StoresNullNotAnEmptyArray()
    {
        await _host.SeedAsync(new InlineThing { Id = 1, Tags = ["alpha"] });

        await Tags.Set(1, []);

        Assert.Null((await _host.Context.Set<InlineThing>().AsNoTracking().SingleAsync(e => e.Id == 1)).Tags);
    }

    [Fact]
    public async Task Set_RejectsMalformedTagsAndWritesNothing()
    {
        await _host.SeedAsync(new InlineThing { Id = 1, Tags = ["alpha"] });

        await Assert.ThrowsAsync<BadRequestException>(() => Tags.Set(1, ["ok", "not ok"]));

        Assert.Equal(["alpha"], await Tags.Get(1));
    }

    [Fact]
    public async Task Set_UnknownRow_IsABadRequest()
    {
        var error = await Assert.ThrowsAsync<BadRequestException>(() => Tags.Set(404, ["alpha"]));

        Assert.Equal("tag-entity-not-found", error.Message);
    }

    [Fact]
    public async Task Get_UntaggedOrMissing_IsEmpty()
    {
        await _host.SeedAsync(new InlineThing { Id = 1 });

        Assert.Empty(await Tags.Get(1));
        Assert.Empty(await Tags.Get(404));
    }

    [Fact]
    public async Task Remove_TakesTheSetDifference()
    {
        await _host.SeedAsync(new InlineThing { Id = 1, Tags = ["alpha", "beta"] });

        await Tags.Remove(1, ["Beta"]);
        Assert.Equal(["alpha"], await Tags.Get(1));

        await Tags.Remove(1, ["gamma"]);   // not carried — nothing to do
        Assert.Equal(["alpha"], await Tags.Get(1));
    }

    [Fact]
    public async Task FilterAny_MatchesAnyTag_AndExcludesUntaggedRows()
    {
        await _host.SeedAsync(
            new InlineThing { Id = 1, Tags = ["alpha"] },
            new InlineThing { Id = 2, Tags = ["beta", "gamma"] },
            new InlineThing { Id = 3, Tags = ["delta"] },
            new InlineThing { Id = 4 });

        var matched = await Tags.FilterAny(_host.Context.Set<InlineThing>(), ["alpha", "gamma"]);

        Assert.Equal([1, 2], (await matched.Select(e => e.Id).ToListAsync()).Order());
    }

    [Fact]
    public async Task FilterAny_NoTags_LeavesTheQueryAlone()
    {
        var query = _host.Context.Set<InlineThing>();

        Assert.Same(query, await Tags.FilterAny(query, []));
    }

    [Fact]
    public async Task Distinct_IsDedupedAndOrdinallySorted()
    {
        await _host.SeedAsync(
            new InlineThing { Id = 1, Tags = ["beta", "alpha"] },
            new InlineThing { Id = 2, Tags = ["alpha"] },
            new InlineThing { Id = 3 });

        Assert.Equal(["alpha", "beta"], await Tags.Distinct());
    }

    [Fact]
    public async Task Hydrate_DoesNothing_TheColumnCameWithTheRow()
    {
        var rows = new List<InlineThing> { new() { Id = 1, Tags = ["alpha"] } };

        await Tags.Hydrate(rows);

        Assert.Equal(["alpha"], rows[0].Tags);
    }

    public void Dispose() => _host.Dispose();
}
