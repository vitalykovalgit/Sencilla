namespace Microsoft.EntityFrameworkCore;

public static class RepoEFIServiceCollectionEx
{
    static Type baseDbContextType = typeof(DbContext);
    static Type dynamicDbContextType = typeof(DynamicDbContext);

    public static IServiceCollection RegisterEFFilters(this IServiceCollection container, Type type)
    {
        if (type.IsAssignableTo(typeof(IBaseEntity)) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            var @event = typeof(EntityReadingEvent<>).MakeGenericType(type);
            var @interface = typeof(IEventHandlerBase<>).MakeGenericType(@event);
            var constraint = typeof(FilterConstraintHandler<>).MakeGenericType(type);
            container.AddScoped(@interface, constraint);

            // Append-only valid-time entities also get the as-of (IFilter.AsOf) range narrowing.
            if (typeof(IEntityAppendOnlyTrack).IsAssignableFrom(type))
                container.AddScoped(@interface, typeof(AppendOnlyTrackReadingHandler<>).MakeGenericType(type));
        }
        return container;
    }


    public static IServiceCollection RegisterEFContexts(this IServiceCollection container, Type type, Action<DbContextOptionsBuilder> configure)
    {
        if (type == dynamicDbContextType) return container;

        if (baseDbContextType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            // Check if DbContext is already registered
            if (container.Any(descriptor => descriptor.ServiceType == type))
                return container; // Already registered, skip

            // Register DbContext dynamically using reflection
            Type dbContextType = type;
            var addDbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethods()
                .First(m => m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 4
                         && m.GetParameters()[1].ParameterType == typeof(Action<DbContextOptionsBuilder>));

            var genericMethod = addDbContextMethod.MakeGenericMethod(dbContextType);
            genericMethod.Invoke(null, [container, configure, ServiceLifetime.Scoped, ServiceLifetime.Scoped]);
        }

        return container;
    }

    public static IServiceCollection RegisterEFRepositoriesForType(this IServiceCollection container, Type type, out bool isAdded) 
    {
        // 
        isAdded = false;
        var typeEntity = typeof(IBaseEntity);
        if (typeEntity.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            // get 
            var entity = type.GetInterfaces()
                             .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

            var customDbContext = type.GetCustomAttribute(typeof(DbContextAttribute<>)) as IDbContextAttribute;
            var key = entity.GetGenericArguments()[0];
            var context = dynamicDbContextType;
            if (customDbContext != null) 
                context = customDbContext.Type;

            RegisterReadRepo(container, type, context, key);
            RegisterCreateRepo(container, type, context, key);
            RegisterUpdateRepo(container, type, context, key);
            RegisterRemoveRepo(container, type, context, key);
            RegisterDeleteRepo(container, type, context, key);

            isAdded = true;
        }

        return container;
    }

    private static void RegisterReadRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register read repo 
        if (typeof(IEntity<>).MakeGenericType(key).IsAssignableFrom(type))
        {
            container.TryAddScoped(
                typeof(IReadRepository<,>).MakeGenericType(type, key),
                typeof(ReadRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddScoped(
                    typeof(IReadRepository<>).MakeGenericType(type),
                    typeof(ReadRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private static void RegisterCreateRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        if (!typeof(IEntityCreateable).IsAssignableFrom(type))
            return;

        // Append-only valid-time entities get the supersede repository (create appends a new version).
        if (typeof(IEntityAppendOnlyTrack).IsAssignableFrom(type))
        {
            container.TryAddScoped(
                typeof(ICreateRepository<,>).MakeGenericType(type, key),
                typeof(AppendOnlyTrackRepository<,,>).MakeGenericType(type, context, key));
            return;
        }

        container.TryAddScoped(
            typeof(ICreateRepository<,>).MakeGenericType(type, key),
            typeof(CreateRepository<,,>).MakeGenericType(type, context, key));

        if (key == typeof(int))
        {
            container.TryAddScoped(
                typeof(ICreateRepository<>).MakeGenericType(type),
                typeof(CreateRepository<,>).MakeGenericType(type, context));
        }
    }

    private static void RegisterUpdateRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // Append-only valid-time entities expose "update" as a supersede (append a new version), even
        // though they don't implement IEntityUpdateable — so the CRUD POST works instead of returning 501.
        if (typeof(IEntityAppendOnlyTrack).IsAssignableFrom(type))
        {
            container.TryAddScoped(
                typeof(IUpdateRepository<,>).MakeGenericType(type, key),
                typeof(AppendOnlyTrackRepository<,,>).MakeGenericType(type, context, key));
            return;
        }

        // register update repo
        if (!typeof(IEntityUpdateable).IsAssignableFrom(type))
            return;

        container.TryAddScoped(
            typeof(IUpdateRepository<,>).MakeGenericType(type, key),
            typeof(UpdateRepository<,,>).MakeGenericType(type, context, key));

        if (key == typeof(int))
        {
            container.TryAddScoped(
                typeof(IUpdateRepository<>).MakeGenericType(type),
                typeof(UpdateRepository<,>).MakeGenericType(type, context));
        }
    }

    private static void RegisterRemoveRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register remvoe repo 
        if (typeof(IEntityRemoveable).IsAssignableFrom(type))
        {
            container.TryAddScoped(
                typeof(IRemoveRepository<,>).MakeGenericType(type, key),
                typeof(RemoveRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddScoped(
                    typeof(IRemoveRepository<>).MakeGenericType(type),
                    typeof(RemoveRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private static void RegisterDeleteRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register delete repo 
        if (typeof(IEntityDeleteable).IsAssignableFrom(type))
        {
            container.TryAddScoped(
                typeof(IDeleteRepository<,>).MakeGenericType(type, key),
                typeof(DeleteRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddScoped(
                    typeof(IDeleteRepository<>).MakeGenericType(type),
                    typeof(DeleteRepository<,>).MakeGenericType(type, context));
            }
        }
    }
}
