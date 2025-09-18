
namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
public class RepositoryRegistrator : ITypeRegistrator
{
    public List<Type> Entities { get; } = new List<Type>();

    public void Register(IServiceCollection container, Type type)
    {
        // 
        var typeEntity = typeof(IBaseEntity);
        if (typeEntity.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            // Add entity to collection 
            Entities.Add(type);

            // get 
            var entity = type.GetInterfaces()
                             .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

            var key = entity.GetGenericArguments()[0];
            var context = typeof(DynamicDbContext);

            
            RegisterReadRepo(container, type, context, key);
            RegisterCreateRepo(container, type, context, key);
            RegisterUpdateRepo(container, type, context, key);
            RegisterRemoveRepo(container, type, context, key);
            RegisterDeleteRepo(container, type, context, key);
        }
    }

    private void RegisterReadRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register read repo 
        if (typeof(IEntity<>).MakeGenericType(key).IsAssignableFrom(type))
        {
            container.TryAddTransient(
                typeof(IReadRepository<,>).MakeGenericType(type, key),
                typeof(ReadRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddTransient(
                    typeof(IReadRepository<>).MakeGenericType(type),
                    typeof(ReadRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterCreateRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register create repo 
        if (typeof(IEntityCreateable).IsAssignableFrom(type))
        {
            container.TryAddTransient(
                typeof(ICreateRepository<,>).MakeGenericType(type, key),
                typeof(CreateRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddTransient(
                    typeof(ICreateRepository<>).MakeGenericType(type),
                    typeof(CreateRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterUpdateRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register update repo 
        if (typeof(IEntityUpdateable).IsAssignableFrom(type))
        {
            container.TryAddTransient(
                typeof(IUpdateRepository<,>).MakeGenericType(type, key),
                typeof(UpdateRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddTransient(
                    typeof(IUpdateRepository<>).MakeGenericType(type),
                    typeof(UpdateRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterRemoveRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register remvoe repo 
        if (typeof(IEntityRemoveable).IsAssignableFrom(type))
        {
            container.TryAddTransient(
                typeof(IRemoveRepository<,>).MakeGenericType(type, key),
                typeof(RemoveRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddTransient(
                    typeof(IRemoveRepository<>).MakeGenericType(type),
                    typeof(RemoveRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterDeleteRepo(IServiceCollection container, Type type, Type context, Type key)
    {
        // register delete repo 
        if (typeof(IEntityDeleteable).IsAssignableFrom(type))
        {
            container.TryAddTransient(
                typeof(IDeleteRepository<,>).MakeGenericType(type, key),
                typeof(DeleteRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.TryAddTransient(
                    typeof(IDeleteRepository<>).MakeGenericType(type),
                    typeof(DeleteRepository<,>).MakeGenericType(type, context));
            }
        }
    }
}
