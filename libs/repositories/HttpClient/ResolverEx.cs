
//using Sencilla.Core;
//using Sencilla.Core.Repo;
//using Sencilla.Impl.Repository.HttpClient;

//namespace Sencilla.Core.Injection
//{
//    public static class ResolverEx
//    {
//        public static void AddHttpClientRepositoriesFor<TEntity, TKey, TContext>(this IResolver builder)
//        {
//            var context = typeof(TContext);
//            var entity = typeof(TEntity);
//            var key = typeof(TKey);

//            if (typeof(IEntity<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IReadRepository<,>).MakeGenericType(entity, key),
//                    typeof(ReadRepository<,,>).MakeGenericType(context, entity, key));
//            }

//            if (typeof(IEntityCreateable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(ICreateRepository<,>).MakeGenericType(entity, key),
//                    typeof(CreateRepository<,,>).MakeGenericType(context, entity, key));
//            }

//            if (typeof(IEntityUpdateable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IUpdateRepository<,>).MakeGenericType(entity, key),
//                    typeof(UpdateRepository<,,>).MakeGenericType(context, entity, key));
//            }

//            if (typeof(IEntityRemoveable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IRemoveRepository<,>).MakeGenericType(entity, key),
//                    typeof(RemoveRepository<,,>).MakeGenericType(context, entity, key));
//            }

//            if (typeof(IEntityDeleteable<TKey>).IsAssignableFrom(entity))
//            {
//                builder.RegisterType(
//                    typeof(IDeleteRepository<,>).MakeGenericType(entity, key),
//                    typeof(DeleteRepository<,,>).MakeGenericType(context, entity, key));
//            }
//        }
//    }
//}
