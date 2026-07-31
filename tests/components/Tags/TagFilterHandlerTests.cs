namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// <c>?tag=</c> composition into the read pipeline. The load-bearing case is the last one: a filter whose tags
/// are all malformed must match NOTHING — silently widening a deliberate filter to "everything" is the worst
/// possible reading of a bad tag, and on a permission-filtered list it would look like a leak.
/// </summary>
public class TagFilterHandlerTests
{
    [Fact]
    public async Task NoTagFilter_LeavesTheQueryUntouched()
    {
        var (handler, repository) = HandlerFor();
        var @event = Reading(new Filter());

        await handler.HandleAsync(@event, default);

        repository.Verify(s => s.FilterAny(It.IsAny<IQueryable<SharedThing>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Tags_AreNormalisedBeforeReachingTheStore()
    {
        var (handler, repository) = HandlerFor();

        await handler.HandleAsync(Reading(new Filter { Tag = [" Alpha ", "BETA"] }), default);

        repository.Verify(s => s.FilterAny(It.IsAny<IQueryable<SharedThing>>(), new[] { "alpha", "beta" }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MalformedTagsAreDropped_ButTheGoodOnesStillFilter()
    {
        var (handler, repository) = HandlerFor();

        await handler.HandleAsync(Reading(new Filter { Tag = ["alpha", "not a tag"] }), default);

        repository.Verify(s => s.FilterAny(It.IsAny<IQueryable<SharedThing>>(), new[] { "alpha" }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AllTagsMalformed_MatchesNothing()
    {
        var (handler, repository) = HandlerFor();
        var @event = Reading(new Filter { Tag = ["not a tag", ""] });

        await handler.HandleAsync(@event, default);

        Assert.Empty(@event.Entities!);
        repository.Verify(s => s.FilterAny(It.IsAny<IQueryable<SharedThing>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    static EntityReadingEvent<SharedThing> Reading(IFilter filter) => new()
    {
        Filter = filter,
        Entities = new[] { new SharedThing { Id = 1 }, new SharedThing { Id = 2 } }.AsQueryable()
    };

    static (TagFilterHandler<SharedThing, int> handler, Mock<ITagRepository<SharedThing, int>> repository) HandlerFor()
    {
        var repository = new Mock<ITagRepository<SharedThing, int>>();

        repository.Setup(s => s.FilterAny(It.IsAny<IQueryable<SharedThing>>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((IQueryable<SharedThing> query, string[] _, CancellationToken _) => query);

        return (new TagFilterHandler<SharedThing, int>(repository.Object), repository);
    }
}
