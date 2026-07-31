namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// Side-table hydration is opt-in on <c>?with=tags</c>, so a list read of a linked/shared entity costs its
/// second query only when the caller wants tags.
/// </summary>
public class TagHydrationHandlerTests : IDisposable
{
    readonly TagTestHost _host = new();

    static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    async Task<LinkedThing> Read(params string[]? with)
    {
        await _host.SeedAsync(new LinkedThing { Id = Id });
        await _host.Tags<LinkedThing, Guid>().Set(Id, ["alpha"]);

        var row = new LinkedThing { Id = Id };
        var @event = new EntityReadEvent<LinkedThing>
        {
            Entities = [row],
            Filter = with == null ? null : new Filter<LinkedThing> { With = with },
        };

        await new TagHydrationHandler<LinkedThing, Guid>(_host.Tags<LinkedThing, Guid>()).HandleAsync(@event, default);

        return row;
    }

    [Theory]
    [InlineData("tags")]
    [InlineData("Tags")]                 // same rule as every other with value: case-insensitive
    public async Task WithTags_Hydrates(string with)
    {
        Assert.Equal(["alpha"], (await Read(with)).Tags);
    }

    [Fact]
    public async Task WithTags_AlongsideRealIncludes_StillHydrates()
    {
        Assert.Equal(["alpha"], (await Read("something", "tags")).Tags);
    }

    [Fact]
    public async Task NoFilterAtAll_DoesNotHydrate() => Assert.Null((await Read(null)).Tags);

    [Fact]
    public async Task AnEmptyWith_DoesNotHydrate() => Assert.Null((await Read()).Tags);

    [Fact]
    public async Task SomeOtherWith_DoesNotHydrate() => Assert.Null((await Read("somethingelse")).Tags);

    public void Dispose() => _host.Dispose();
}
