namespace Sencilla.Component.Tags;

/// <summary>
/// Wires one <see cref="ITagRepository{TEntity,TKey}"/> per taggable entity, picked from which storage interface the
/// entity implements, plus the pipeline handlers that repository needs. Runs from BOTH the <c>AddSencilla()</c>
/// [AutoDiscovery] scan and the <c>AddSencillaTags()</c> fallback loop, so every registration is idempotent —
/// a duplicate descriptor would filter or hydrate twice.
/// </summary>
public class TagRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false })
            return;
        if (!type.IsAssignableTo(typeof(IEntityTaggable)))
            return;

        var key = type.GetInterfaces()
                      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
                      ?.GetGenericArguments()[0];

        // A taggable type that is not an entity has nothing to tag against — ignore rather than crash startup.
        if (key == null)
            return;

        var repositoryContract = typeof(ITagRepository<,>).MakeGenericType(type, key);
        var repository = ResolveRepository(type, key);

        AddOnce(container, repository, repository);
        AddOnce(container, repositoryContract, repository);

        // ?tag= filtering — every repository.
        AddOnce(container,
            typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityReadingEvent<>).MakeGenericType(type)),
            typeof(TagFilterHandler<,>).MakeGenericType(type, key));

        // Hydration — side-table repositories only; the inline column needs none.
        if (!type.IsAssignableTo(typeof(IEntityTaggableInline)))
            AddOnce(container,
                typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityReadEvent<>).MakeGenericType(type)),
                typeof(TagHydrationHandler<,>).MakeGenericType(type, key));

        // Orphan sweeping — the shared table has no FK to cascade with.
        if (type.IsAssignableTo(typeof(IEntityTaggableShared)))
            AddOnce(container,
                typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityDeletingEvent<>).MakeGenericType(type)),
                typeof(SharedTagCleanupHandler<,>).MakeGenericType(type, key));
    }

    /// <summary>
    /// The repository implementation for an entity's declared strategy. Exactly one storage interface must be
    /// implemented, and a linked entity must ship its link entity — both are authoring mistakes that fail here,
    /// at startup, rather than as a confusing 500 on the first tag read.
    /// </summary>
    static Type ResolveRepository(Type type, Type key)
    {
        var inline = type.IsAssignableTo(typeof(IEntityTaggableInline));
        var linked = type.IsAssignableTo(typeof(IEntityTaggableLinked));
        var shared = type.IsAssignableTo(typeof(IEntityTaggableShared));

        if (inline && !linked && !shared)
            return typeof(TagInlineRepository<,>).MakeGenericType(type, key);

        if (linked && !inline && !shared)
            return typeof(TagLinkedRepository<,,>).MakeGenericType(type, key, LinkTypeOf(type, key));

        if (shared && !inline && !linked)
            return typeof(TagSharedRepository<,>).MakeGenericType(type, key);

        throw new InvalidOperationException(
            $"{type.Name} implements IEntityTaggable but not exactly one of IEntityTaggableInline / " +
            "IEntityTaggableLinked / IEntityTaggableShared — a taggable entity must declare where its tags live.");
    }

    /// <summary>
    /// The concrete link entity for a linked entity: a non-generic subclass of
    /// <c>EntityTagLink&lt;TEntity, TKey&gt;</c>. Non-generic is mandatory — Sencilla's auto-discovery skips
    /// generic types, so a generic link would silently have no repository and no table.
    /// </summary>
    static Type LinkTypeOf(Type type, Type key)
    {
        var linkBase = typeof(EntityTagLink<,>).MakeGenericType(type, key);

        var link = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeTypes)
            .FirstOrDefault(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false } && linkBase.IsAssignableFrom(t));

        return link ?? throw new InvalidOperationException(
            $"{type.Name} is IEntityTaggableLinked but no non-generic link entity deriving from " +
            $"EntityTagLink<{type.Name}, {key.Name}> was found — add e.g. " +
            $"'public class {type.Name}Tag : EntityTagLink<{type.Name}, {key.Name}> {{ }}'.");
    }

    static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; }
    }

    static void AddOnce(IServiceCollection container, Type service, Type impl)
    {
        if (!container.Any(d => d.ServiceType == service && d.ImplementationType == impl))
            container.AddScoped(service, impl);
    }
}
