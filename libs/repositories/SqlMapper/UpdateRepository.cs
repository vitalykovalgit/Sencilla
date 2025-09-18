//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//using Sencilla.Core.Entity;
//using Sencilla.Core.Repo;
//using Sencilla.Infrastructure.SqlMapper.Impl;
//using Sencilla.Core.Injection;

//namespace Sencilla.Impl.Repository.SqlMapper
//{
//    public class UpdateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>,
//        IUpdateRepository<TEntity, TKey>
//        where TEntity : class, IEntityUpdateable<TKey>, new()
//        where TContext : DbContext
//    {
//        public UpdateRepository(IServiceProvider resolver) : base(resolver)
//        {
//        }

//        public TEntity Update(TEntity entity, string ruleSet = null)
//        {
//            if (entity != null)
//            {
//                using (var context = Resolve<TContext>())
//                {
//                    entity.UpdatedDate = DateTime.UtcNow;

//                    context.Set<TEntity, TKey>().Update(entity, entity.Id);
//                    context.Commit();
//                }
//            }
//            return entity;
//        }

//        public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken? token = null, string ruleSet = null)
//        {
//            if (entity != null)
//            {
//                using (var context = Resolve<TContext>())
//                {
//                    entity.UpdatedDate = DateTime.UtcNow;

//                    await context.Set<TEntity, TKey>().UpdateAsync(entity, entity.Id, token);
//                    context.Commit();
//                }
//            }
//            return entity;
//        }

//        public IEnumerable<TEntity> Update(IEnumerable<TEntity> entities)
//        {
//            if (entities != null)
//            {
//                using (var context = Resolve<TContext>())
//                {
//                    foreach (var entity in entities)
//                    {
//                        entity.UpdatedDate = DateTime.UtcNow;
//                        context.Set<TEntity, TKey>().Update(entity, entity.Id);
//                    }

//                    context.Commit();
//                }
//            }

//            return entities;
//        }

//        public async Task<IEnumerable<TEntity>> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken? token = null)
//        {
//            if (entities != null)
//            {
//                using (var context = Resolve<TContext>())
//                {
//                    foreach (var entity in entities)
//                    {
//                        entity.UpdatedDate = DateTime.UtcNow;
//                        await context.Set<TEntity, TKey>().UpdateAsync(entity, entity.Id);
//                    }

//                    context.Commit();
//                }
//            }
//            return entities;
//        }
//    }
//}
