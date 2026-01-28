
using Microsoft.Identity.Client;

namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class UpdateRepository<TEntity, TContext>(RepositoryDependency dependency, TContext context)
    : UpdateRepository<TEntity, TContext, int>(dependency, context), IUpdateRepository<TEntity>
    where TEntity : class, IEntity<int>, IEntityUpdateable, new()
    where TContext : DbContext;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class UpdateRepository<TEntity, TContext, TKey>(RepositoryDependency dependency, TContext context)
    : ReadRepository<TEntity, TContext, TKey>(dependency, context), IUpdateRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityUpdateable, new()
    where TContext : DbContext
{
    public async Task<TEntity?> Update(TEntity entity, CancellationToken token = default)
    {
        return (await Update([entity])).FirstOrDefault();
    }

    //public Task<int> ExecuteUpdateAsync(Action<IQueryable<TEntity>> updator, CancellationToken token = default)
    //{
    //    var query = this.DbContext.Query<TEntity>();
    //    updator(query);
    //    return query.Where(e => e != null).ExecuteUpdateAsync(s => s.SetProperty(e => e. = ), token);
    //}

    public Task<IEnumerable<TEntity>> Update(params TEntity[] entities)
    {
        return Update(entities, CancellationToken.None);
    }

    public void Detach(IEnumerable<TEntity> entities)
    {
        // Detach entities from context to avoid tracking issues
        foreach (var entity in entities) 
        {
            if (entity != null)
                DbContext.Entry(entity).State = EntityState.Detached;
        }
    }

    public void ClearChangeTracker()
    {
        DbContext.ChangeTracker.Clear();
    }

    public async Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        // Notify before updating 
        var query = entities.AsQueryable();
        var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = query };
        await D.Events.PublishAsync(eventUpdating, token);

        // TODO: Move to trackable 
        foreach (var e in query)
        {
            if (e is IEntityUpdateableTrack track)
                track.UpdatedDate = DateTime.UtcNow;
        }

        //try
        //{
            DbContext.UpdateRange(query);
            await Save(token);
            context.ChangeTracker.Clear();
        //}
        //finally
        //{
            // Do not do like this!!!
            // context.ChangeTracker.Clear();
        //}

        // Notify that entity updated
        var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = query };
        await D.Events.PublishAsync(eventUpdated, token);

        return query;
    }
}

