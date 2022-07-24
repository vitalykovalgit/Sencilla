using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    /// <summary>
    /// 
    /// </summary>
    public class RepositoryRegistrator : ITypeRegistrator
    {
        public List<Type> Entities { get; } = new List<Type>();

        public void Register(IRegistrator container, Type type)
        {
            // 
            var typeEntity = typeof(IBaseEntity);
            if (typeEntity.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            {
                // Add entity to collection 
                Entities.Add(type);

                // get 
                var entity = type.GetInterfaces()
                                 .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

                var key = entity.GetGenericArguments()[0];
                var context = typeof(DynamicDbContext);

                // register read repo 
                if (typeof(IEntity<>).MakeGenericType(key).IsAssignableFrom(type))
                {
                    container.RegisterType(
                        typeof(IReadRepository<,>).MakeGenericType(type, key),
                        typeof(ReadRepository<,,>).MakeGenericType(type, context, key));
                }
            }
        }
    }
}
