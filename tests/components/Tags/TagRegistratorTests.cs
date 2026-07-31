namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// Repository selection and the two authoring mistakes that must fail at STARTUP: a taggable entity that never says
/// where its tags live, and a linked entity with no link table. Both would otherwise surface as a confusing 500
/// on the first tag read.
/// </summary>
public class TagRegistratorTests
{
    [Fact]
    public void Inline_GetsTheInlineStore()
        => Assert.Equal(typeof(TagInlineRepository<InlineThing, int>), StoreFor<InlineThing, int>());

    [Fact]
    public void Linked_GetsTheLinkedStore_ClosedOverItsLinkEntity()
        => Assert.Equal(typeof(TagLinkedRepository<LinkedThing, Guid, LinkedThingTag>), StoreFor<LinkedThing, Guid>());

    [Fact]
    public void Shared_GetsTheSharedStore()
        => Assert.Equal(typeof(TagSharedRepository<SharedThing, int>), StoreFor<SharedThing, int>());

    [Fact]
    public void Filtering_IsRegisteredForEveryStore()
    {
        Assert.NotNull(Handler<InlineThing>(typeof(EntityReadingEvent<InlineThing>)));
        Assert.NotNull(Handler<SharedThing>(typeof(EntityReadingEvent<SharedThing>)));
    }

    [Fact]
    public void Hydration_IsRegisteredOnlyForSideTableStores()
    {
        // The inline column arrives with the row; an entity with no handler costs no dispatch.
        Assert.Null(Handler<InlineThing>(typeof(EntityReadEvent<InlineThing>)));
        Assert.Equal(typeof(TagHydrationHandler<SharedThing, int>), Handler<SharedThing>(typeof(EntityReadEvent<SharedThing>)));
    }

    [Fact]
    public void OrphanSweeping_IsRegisteredOnlyForTheSharedStore()
    {
        // A linked table cascades, an inline column goes with its row — only the FK-less shared table leaks.
        Assert.Equal(typeof(SharedTagCleanupHandler<SharedThing, int>), Handler<SharedThing>(typeof(EntityDeletingEvent<SharedThing>)));
        Assert.Null(Handler<LinkedThing>(typeof(EntityDeletingEvent<LinkedThing>)));
    }

    [Fact]
    public void Registration_IsIdempotent()
    {
        // Both the AddSencilla() scan and AddSencillaTags() may register the same type; a duplicate would filter
        // (and hydrate) twice.
        var services = new ServiceCollection();
        new TagRegistrator().Register(services, typeof(SharedThing));
        new TagRegistrator().Register(services, typeof(SharedThing));

        Assert.Single(services.Where(d => d.ServiceType == typeof(ITagRepository<SharedThing, int>)));
        Assert.Single(services.Where(d => d.ServiceType == typeof(IEventHandlerBase<EntityReadingEvent<SharedThing>>)));
    }

    [Fact]
    public void NoStrategy_FailsAtStartup()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new TagRegistrator().Register(new ServiceCollection(), typeof(StrategylessThing)));

        Assert.Contains("exactly one", error.Message);
    }

    [Fact]
    public void TwoStrategies_FailAtStartup()
        => Assert.Throws<InvalidOperationException>(() => new TagRegistrator().Register(new ServiceCollection(), typeof(TwoStrategyThing)));

    [Fact]
    public void LinkedWithoutLinkEntity_FailsAtStartupWithTheFix()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new TagRegistrator().Register(new ServiceCollection(), typeof(LinklessThing)));

        Assert.Contains("LinklessThingTag : EntityTagLink<LinklessThing, Int32>", error.Message);
    }

    [Fact]
    public void NonTaggableAndAbstractTypes_AreIgnored()
    {
        var services = new ServiceCollection();

        new TagRegistrator().Register(services, typeof(string));
        new TagRegistrator().Register(services, typeof(EntityTagLink<LinkedThing, Guid>));

        Assert.Empty(services);
    }

    static Type? StoreFor<TEntity, TKey>() where TEntity : class, IEntity<TKey>
    {
        var services = new ServiceCollection();
        new TagRegistrator().Register(services, typeof(TEntity));

        return services.FirstOrDefault(d => d.ServiceType == typeof(ITagRepository<TEntity, TKey>))?.ImplementationType;
    }

    static Type? Handler<TEntity>(Type eventType)
    {
        var services = new ServiceCollection();
        new TagRegistrator().Register(services, typeof(TEntity));

        var contract = typeof(IEventHandlerBase<>).MakeGenericType(eventType);
        return services.FirstOrDefault(d => d.ServiceType == contract)?.ImplementationType;
    }
}
