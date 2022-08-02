
//using Sencilla.Core;
//using Sencilla.Impl.Repository.SqlMapper;

//namespace Sencilla.Core.Injection
//{
//    public static class ResolverEx
//    {
//        public static void AddRepositoriesFor<TEntity, TKey, TContext>(this IResolver builder)
//        {
//            var context = typeof(TContext);
//            var entity = typeof(TEntity);
//            var key = typeof(TKey);

//            if (typeof(IEntity<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IReadRepository<,>).MakeGenericType(entity, key),
//                    typeof(ReadRepository<,,>).MakeGenericType(entity, context, key));
//            }

//            if (typeof(IEntityCreateable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(ICreateRepository<,>).MakeGenericType(entity, key),
//                    typeof(CreateRepository<,,>).MakeGenericType(entity, context, key));
//            }

//            if (typeof(IEntityUpdateable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IUpdateRepository<,>).MakeGenericType(entity, key),
//                    typeof(UpdateRepository<,,>).MakeGenericType(entity, context, key));
//            }

//            if (typeof(IEntityRemoveable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IRemoveRepository<,>).MakeGenericType(entity, key),
//                    typeof(RemoveRepository<,,>).MakeGenericType(entity, context, key));
//            }

//            if (typeof(IEntityDeleteable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IDeleteRepository<,>).MakeGenericType(entity, key),
//                    typeof(DeleteRepository<,,>).MakeGenericType(entity, context, key));
//            }
//        }
//    }
//}
