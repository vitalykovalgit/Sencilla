
namespace Sencilla.Repository.EntityFramework
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class CreateRepository<TEntity, TContext> : CreateRepository<TEntity, TContext, int>, ICreateRepository<TEntity>
       where TEntity : class, IEntity<int>, IEntityCreateable, new()
       where TContext : DbContext
    {
        public CreateRepository(RepositoryDependency dependency, TContext context): base(dependency, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class CreateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, ICreateRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, IEntityCreateable, new()
           where TContext : DbContext
    {
        public CreateRepository(RepositoryDependency dependency, TContext context): base(dependency, context)
        {
        }

        public async Task<TEntity?> Create(TEntity entity, CancellationToken token = default)
        {
            return (await Create(new[] { entity })).FirstOrDefault();
        }

        public Task<IEnumerable<TEntity>> Create(params TEntity[] entities)
        {
            return Create(entities, CancellationToken.None);
        }

        public async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            // Check constarints 
            var query = entities.AsQueryable();

            var @event = new EntityCreatingEvent<TEntity> { Entities = query };
            await D.Events.PublishAsync(@event);
            //foreach (var constraint in Constraints)
            //    query = await constraint.Apply(query, /*create action*/);

            // Validate 

            // update creation date 
            foreach (var e in entities)
            {
                if (e is IEntityCreateableTrack)
                   ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;
            }

            // Add to context and save 
            DbContext.AddRange(entities);
            await Save(token);
            return entities;
        }
    }
}
