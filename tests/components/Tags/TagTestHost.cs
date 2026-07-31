using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sencilla.Repository.EntityFramework;

namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// A miniature host: the real EF repositories, the real registrator, the real model configurator and a real
/// (Sqlite) database — because the tag repositories are now EF repositories, and the parts worth testing are
/// exactly the parts a mock cannot answer: whether the generic <c>EF.Property</c> access, the projections, the
/// DISTINCT push-down and the bulk delete actually translate to SQL.
///
/// <para>Each host gets its own in-memory database but shares one EF model (cached per context type), which is
/// why every test entity is registered up front.</para>
/// </summary>
public sealed class TagTestHost : IDisposable
{
    static readonly Type[] Entities =
    [
        typeof(InlineThing), typeof(LinkedThing), typeof(LinkedThingTag), typeof(SharedThing), typeof(EntityTag)
    ];

    /// <summary>
    /// DynamicDbContext maps whatever is in this static list, so the test entities have to join it once for the
    /// process. Additive and idempotent — parallel test classes see the same complete model.
    /// </summary>
    static TagTestHost()
    {
        foreach (var entity in Entities)
            if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(entity))
                RepositoryEntityFrameworkBootstrap.Entities.Add(entity);
    }

    readonly ServiceProvider _services;

    public DynamicDbContext Context { get; }

    /// <summary>Parents whose update pipeline ran — how a tag write's "touch" of its parent is observed.</summary>
    public List<object> Touched { get; } = [];

    public TagTestHost()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IEntityModelConfigurator, TagsModelConfigurator>();
        services.AddDbContext<DynamicDbContext>(o => o.UseSqlite($"DataSource=tags_{Guid.NewGuid():N};Mode=Memory;Cache=Shared"));

        services.TryAddScoped<RepositoryDependency>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddScoped<IEventMiddleware, InMemoryMiddleware>();
        services.AddScoped(_ => new Mock<ICommandDispatcher>().Object);

        foreach (var entity in Entities)
            services.RegisterEFRepositoriesForType(entity, out _);

        var registrator = new TagRegistrator();
        foreach (var entity in Entities)
            registrator.Register(services, entity);

        // Observe the parent touch through the update pipeline itself, so the assertion holds whether or not
        // the entity carries a tracked UpdatedDate.
        services.AddScoped<IEventHandlerBase<EntityUpdatingEvent<LinkedThing>>>(_ => new TouchSpy<LinkedThing>(Touched));
        services.AddScoped<IEventHandlerBase<EntityUpdatingEvent<SharedThing>>>(_ => new TouchSpy<SharedThing>(Touched));

        _services = services.BuildServiceProvider();

        Context = _services.GetRequiredService<DynamicDbContext>();
        Context.Database.OpenConnection();
        Context.Database.EnsureCreated();
    }

    public ITagRepository<TEntity, TKey> Tags<TEntity, TKey>() where TEntity : class, IEntity<TKey>
        => _services.GetRequiredService<ITagRepository<TEntity, TKey>>();

    public T Get<T>() where T : notnull => _services.GetRequiredService<T>();

    public async Task SeedAsync(params object[] rows)
    {
        Context.AddRange(rows);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        Context.Database.CloseConnection();
        _services.Dispose();
    }

    sealed class TouchSpy<TEntity>(List<object> touched) : IEventHandler<EntityUpdatingEvent<TEntity>>
    {
        public Task HandleAsync(EntityUpdatingEvent<TEntity> @event, CancellationToken token)
        {
            touched.Add(typeof(TEntity));
            return Task.CompletedTask;
        }
    }
}
