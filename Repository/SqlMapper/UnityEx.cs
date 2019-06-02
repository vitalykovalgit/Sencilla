
using Sencilla.Core.Entity;
using Sencilla.Core.Injection;
using Sencilla.Core.Repo;
using Sencilla.Impl.Repository.SqlMapper;

namespace Unity
{
    public static class UnityEx
    {
        public static void SencillaUseSqlMapperRepository<TEntity, TContext, TKey>(this IResolver container)
        {
            var context = typeof(TContext);
            var entity = typeof(TEntity);
            var key = typeof(TKey);

            if (typeof(IEntity<TKey>).IsAssignableFrom(entity))
            {
                container.RegisterType(
                    typeof(IReadRepository<,>).MakeGenericType(entity, key),
                    typeof(ReadRepository<,,>).MakeGenericType(entity, context, key));
            }

            if (typeof(IEntityCreateable<TKey>).IsAssignableFrom(entity))
            {
                container.RegisterType(
                    typeof(ICreateRepository<,>).MakeGenericType(entity, key),
                    typeof(CreateRepository<,,>).MakeGenericType(entity, context, key));
            }

            if (typeof(IEntityUpdateable<TKey>).IsAssignableFrom(entity))
            {
                container.RegisterType(
                    typeof(IUpdateRepository<,>).MakeGenericType(entity, key),
                    typeof(UpdateRepository<,,>).MakeGenericType(entity, context, key));
            }

            if (typeof(IEntityRemoveable<TKey>).IsAssignableFrom(entity))
            {
                container.RegisterType(
                    typeof(IRemoveRepository<,>).MakeGenericType(entity, key),
                    typeof(RemoveRepository<,,>).MakeGenericType(entity, context, key));
            }

            if (typeof(IEntityDeleteable<TKey>).IsAssignableFrom(entity))
            {
                container.RegisterType(
                    typeof(IDeleteRepository<,>).MakeGenericType(entity, key),
                    typeof(DeleteRepository<,,>).MakeGenericType(entity, context, key));
            }
        }
    }
}
