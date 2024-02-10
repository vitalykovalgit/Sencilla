
namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
public class RepositoryRegistrator : ITypeRegistrator
{
    public List<Type> Entities { get; } = new List<Type>();

    public Dictionary<string, List<Type>> ContextEntitiesPairs = new Dictionary<string, List<Type>>()
    {
        { nameof(DynamicDbContext), new List<Type>() },
        { nameof(UserInfoDynamicDbContext), new List<Type>() },
    };

    public void Register(IContainer container, Type type)
    {
        //
        var typeEntity = typeof(IBaseEntity);

        if (typeEntity.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            var entity = GetEntityType(type);
            var context = typeof(DynamicDbContext);
            var key = entity.GetGenericArguments()[0];

            var dynamicContextAttr = type.GetCustomAttributes(typeof(DynamicContextAttribute), true)
                .FirstOrDefault() as DynamicContextAttribute;

            var contextName = dynamicContextAttr == null ? nameof(DynamicDbContext) : nameof(UserInfoDynamicDbContext);

            ContextEntitiesPairs[contextName].Add(type);

            RegisterRepositories(container, type, context, key);
        }
    }

    private Type GetEntityType(Type type)
    {
        return type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
    }

    private void RegisterRepositories(IContainer container, Type type, Type context, Type key)
    {
        RegisterReadRepo(container, type, context, key);
        RegisterCreateRepo(container, type, context, key);
        RegisterUpdateRepo(container, type, context, key);
        RegisterRemoveRepo(container, type, context, key);
        RegisterDeleteRepo(container, type, context, key);
    }

    private void RegisterReadRepo(IContainer container, Type type, Type context, Type key)
    {
        // register read repo 
        if (typeof(IEntity<>).MakeGenericType(key).IsAssignableFrom(type))
        {
            container.RegisterType(
                typeof(IReadRepository<,>).MakeGenericType(type, key),
                typeof(ReadRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.RegisterType(
                    typeof(IReadRepository<>).MakeGenericType(type),
                    typeof(ReadRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterCreateRepo(IContainer container, Type type, Type context, Type key)
    {
        // register create repo 
        if (typeof(IEntityCreateable).IsAssignableFrom(type))
        {
            container.RegisterType(
                typeof(ICreateRepository<,>).MakeGenericType(type, key),
                typeof(CreateRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.RegisterType(
                    typeof(ICreateRepository<>).MakeGenericType(type),
                    typeof(CreateRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterUpdateRepo(IContainer container, Type type, Type context, Type key)
    {
        // register update repo 
        if (typeof(IEntityUpdateable).IsAssignableFrom(type))
        {
            container.RegisterType(
                typeof(IUpdateRepository<,>).MakeGenericType(type, key),
                typeof(UpdateRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.RegisterType(
                    typeof(IUpdateRepository<>).MakeGenericType(type),
                    typeof(UpdateRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterRemoveRepo(IContainer container, Type type, Type context, Type key)
    {
        // register remvoe repo 
        if (typeof(IEntityRemoveable).IsAssignableFrom(type))
        {
            container.RegisterType(
                typeof(IRemoveRepository<,>).MakeGenericType(type, key),
                typeof(RemoveRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.RegisterType(
                    typeof(IRemoveRepository<>).MakeGenericType(type),
                    typeof(RemoveRepository<,>).MakeGenericType(type, context));
            }
        }
    }

    private void RegisterDeleteRepo(IContainer container, Type type, Type context, Type key)
    {
        // register delete repo 
        if (typeof(IEntityDeleteable).IsAssignableFrom(type))
        {
            container.RegisterType(
                typeof(IDeleteRepository<,>).MakeGenericType(type, key),
                typeof(DeleteRepository<,,>).MakeGenericType(type, context, key));

            if (key == typeof(int))
            {
                container.RegisterType(
                    typeof(IDeleteRepository<>).MakeGenericType(type),
                    typeof(DeleteRepository<,>).MakeGenericType(type, context));
            }
        }
    }
}
