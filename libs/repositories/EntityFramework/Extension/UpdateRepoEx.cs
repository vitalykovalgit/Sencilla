namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Extension methods for UpdateRepository to execute bulk updates by entity IDs
/// </summary>
/// <example>
/// Usage example:
/// <code>
/// Expression&lt;Func&lt;SetPropertyCalls&lt;MyEntity&gt;, SetPropertyCalls&lt;MyEntity&gt;&gt;&gt; setters = 
///     s =&gt; s.SetProperty(e =&gt; e.State, 1)
///           .SetProperty(e =&gt; e.Name, "name");
/// 
/// int affected = await updateRepo.ExecuteUpdate(entities, setters);
/// </code>
/// </example>
public static class UpdateRepoEx
{
    /// <summary>
    /// Executes an update query on entities matching the provided collection by their IDs (default int key).
    /// Internally translates to: Query.Where(e => e.Id in entities).ExecuteUpdateAsync(setPropertyCalls)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="repository">The update repository</param>
    /// <param name="entities">Collection of entities to update (only IDs are extracted and used)</param>
    /// <param name="setPropertyCalls">Lambda expression defining property updates using EF Core's SetProperty method</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of entities affected by the update</returns>
    public static Task<int> ExecuteUpdateAsync<TEntity>(this IUpdateRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default)
        where TEntity : class, IEntity<int>
    {
        return ExecuteUpdateAsync<TEntity, int>(repository, entities, setPropertyCalls, token);
    }

    /// <summary>
    /// Executes an update query on entities matching the provided collection by their IDs.
    /// Internally translates to: Query.Where(e => e.Id in entities).ExecuteUpdateAsync(setPropertyCalls)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="repository">The update repository</param>
    /// <param name="entities">Collection of entities to update (only IDs are extracted and used)</param>
    /// <param name="setPropertyCalls">Lambda expression defining property updates using EF Core's SetProperty method</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of entities affected by the update</returns>
    /// <remarks>
    /// This method generates an efficient SQL UPDATE statement with a WHERE IN clause.
    /// The setPropertyCalls parameter should be a lambda expression like:
    /// s => s.SetProperty(e => e.Property1, value1).SetProperty(e => e.Property2, value2)
    /// </remarks>
    public static Task<int> ExecuteUpdateAsync<TEntity, TKey>(
        this IUpdateRepository<TEntity, TKey> repository, 
        IEnumerable<TEntity> entities,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity<TKey>
    {
        var ids = entities.Select(e => e.Id).ToList();
        var query = repository.Query.Where(e => ids.Contains(e.Id)).ExecuteUpdateAsync(s => setPropertyCalls(s));
        return query;
    }

    public static Task<int> ExecuteUpdateAsync<TEntity>(
        this IUpdateRepository<TEntity> repository,
        IEnumerable<int> ids,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity
    {
        var query = repository.Query.Where(e => ids.Contains(e.Id)).ExecuteUpdateAsync(s => setPropertyCalls(s));
        return query;
    }

    public static Task<int> ExecuteUpdateAsync<TEntity, TKey>(
        this IUpdateRepository<TEntity, TKey> repository,
        IEnumerable<TKey> ids,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity<TKey>
    {
        var query = repository.Query.Where(e => ids.Contains(e.Id)).ExecuteUpdateAsync(s => setPropertyCalls(s));
        return query;
    }

    /// <summary>
    /// Executes an update query on a single entity by its ID (default int key).
    /// Internally translates to: Query.Where(e => e.Id == id).ExecuteUpdateAsync(setPropertyCalls)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="repository">The update repository</param>
    /// <param name="id">The ID of the entity to update</param>
    /// <param name="setPropertyCalls">Lambda expression defining property updates using EF Core's SetProperty method</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of entities affected by the update (0 or 1)</returns>
    public static Task<int> ExecuteUpdateAsync<TEntity>(
        this IUpdateRepository<TEntity> repository,
        int id,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity<int>
    {
        return ExecuteUpdateAsync<TEntity, int>(repository, id, setPropertyCalls, token);
    }

    /// <summary>
    /// Executes an update query on a single entity by its ID.
    /// Internally translates to: Query.Where(e => e.Id == id).ExecuteUpdateAsync(setPropertyCalls)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="repository">The update repository</param>
    /// <param name="id">The ID of the entity to update</param>
    /// <param name="setPropertyCalls">Lambda expression defining property updates using EF Core's SetProperty method</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of entities affected by the update (0 or 1)</returns>
    /// <remarks>
    /// This method generates an efficient SQL UPDATE statement with a WHERE clause.
    /// The setPropertyCalls parameter should be a lambda expression like:
    /// s => s.SetProperty(e => e.Property1, value1).SetProperty(e => e.Property2, value2)
    /// </remarks>
    public static Task<int> ExecuteUpdateAsync<TEntity, TKey>(
        this IUpdateRepository<TEntity, TKey> repository,
        TKey id,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity<TKey>
    {
        var query = repository.Query.Where(e => e.Id!.Equals(id)).ExecuteUpdateAsync(s => setPropertyCalls(s));
        return query;
    }

    /// <summary>
    /// Executes an update query on a single entity (extracts ID from entity).
    /// Internally translates to: Query.Where(e => e.Id == entity.Id).ExecuteUpdateAsync(setPropertyCalls)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="repository">The update repository</param>
    /// <param name="entity">The entity to update (only ID is extracted and used)</param>
    /// <param name="setPropertyCalls">Lambda expression defining property updates using EF Core's SetProperty method</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of entities affected by the update (0 or 1)</returns>
    public static Task<int> ExecuteUpdateAsync<TEntity>(
        this IUpdateRepository<TEntity> repository,
        TEntity entity,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity<int>
    {
        return ExecuteUpdateAsync<TEntity, int>(repository, entity, setPropertyCalls, token);
    }

    /// <summary>
    /// Executes an update query on a single entity (extracts ID from entity).
    /// Internally translates to: Query.Where(e => e.Id == entity.Id).ExecuteUpdateAsync(setPropertyCalls)
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="repository">The update repository</param>
    /// <param name="entity">The entity to update (only ID is extracted and used)</param>
    /// <param name="setPropertyCalls">Lambda expression defining property updates using EF Core's SetProperty method</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of entities affected by the update (0 or 1)</returns>
    /// <remarks>
    /// This method generates an efficient SQL UPDATE statement with a WHERE clause.
    /// The setPropertyCalls parameter should be a lambda expression like:
    /// s => s.SetProperty(e => e.Property1, value1).SetProperty(e => e.Property2, value2)
    /// </remarks>
    public static Task<int> ExecuteUpdateAsync<TEntity, TKey>(
        this IUpdateRepository<TEntity, TKey> repository,
        TEntity entity,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken token = default) where TEntity : class, IEntity<TKey>
    {
        var query = repository.Query.Where(e => e.Id!.Equals(entity.Id)).ExecuteUpdateAsync(s => setPropertyCalls(s));
        return query;
    }
}

