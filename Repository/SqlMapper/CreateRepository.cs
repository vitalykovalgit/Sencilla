using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Sencilla.Infrastructure.SqlMapper.Impl;
using Sencilla.Core.Entity;
using Sencilla.Core.Repo;

namespace Sencilla.Impl.Repository.SqlMapper
{
    public class CreateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, ICreateRepository<TEntity, TKey>
        where TEntity : class, IEntityCreateable<TKey>, new()
        where TContext : DbContext
    {
        public TEntity Create(TEntity entity)
        {
            if (entity == null)
                return null;

            using (var context = Resolve<TContext>())
            {
                entity.CreatedDate = DateTime.UtcNow;

                var key = context.Set<TEntity, TKey>().Insert(entity);
                context.Commit();

                entity.Id = key;
            }
            return entity;
        }

        public IEnumerable<TEntity> Create(IEnumerable<TEntity> entities)
        {
            using (var context = Resolve<TContext>())
            {
                foreach (var entity in entities)
                {
                    entity.CreatedDate = DateTime.UtcNow;

                    var key = context.Set<TEntity, TKey>().Insert(entity);
                    entity.Id = key;
                }
                context.Commit();
            }
            return entities;
        }

        public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken? token)
        {
            if (entity == null)
                return null;

            using (var context = Resolve<TContext>())
            {
                entity.CreatedDate = DateTime.UtcNow;

                var key = await context.Set<TEntity, TKey>().InsertAsync(entity, token);
                context.Commit();

                entity.Id = key;
            }
            return entity;
        }

        public Task<IEnumerable<TEntity>> CreateAsync(IEnumerable<TEntity> entities, CancellationToken? token)
        {
            //if (entities != null)
            //{
            //    foreach (var entity in entities)
            //        await CreateAsync(entity, token);
            //}

            //return entities;
            throw new NotImplementedException();
        }
    }
}
