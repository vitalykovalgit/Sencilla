namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class CrudApiController<TEntity>(IServiceProvider resolver) : CrudApiController<TEntity, int>(resolver) where TEntity : class, IEntity<int>, new()
{
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class CrudApiController<TEntity, TKey>(IServiceProvider resolver) : ApiController(resolver) where TEntity : class, IEntity<TKey>, new()
{
    private static readonly UseCachingAttribute? CachingAttribute = typeof(TEntity).GetCustomAttribute<UseCachingAttribute>();

    [HttpGet, Route("")]
    public virtual async Task<IActionResult> GetAll(Filter<TEntity> filter, CancellationToken token)
    {
        if (CachingAttribute != null)
        {
            var cache = resolver.GetService<IMemoryCache>();
            if (cache != null)
            {
                var cacheKey = $"crud_getall_{typeof(TEntity).Name}_{filter.GetHashCode()}";
                var result = await cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CachingAttribute.Expiration;
                    var repo = resolver.GetService<IReadRepository<TEntity, TKey>>();
                    return await repo!.GetAll(filter, token);
                });
                return Ok(result);
            }
        }

        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetAll(filter, token));
    }

    [HttpGet, Route("{id}")]
    public virtual async Task<IActionResult> GetById(TKey id, Filter<TEntity> filter)
    {
        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetById(id));
    }

    [HttpGet, Route("count")]
    public virtual async Task<IActionResult> GetCount(Filter<TEntity> filter, CancellationToken token)
    {
        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetCount(filter, token));
    }

    [HttpGet, Route("sum")]
    public virtual async Task<IActionResult> GetSum(Filter<TEntity> filter, CancellationToken token)
    {
        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetSum(filter, token));
    }

    [HttpGet, Route("max")]
    public virtual async Task<IActionResult> GetMax(Filter<TEntity> filter, CancellationToken token)
    {
        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetMax(filter, token));
    }

    [HttpGet, Route("min")]
    public virtual async Task<IActionResult> GetMin(Filter<TEntity> filter, CancellationToken token)
    {
        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetMin(filter, token));
    }

    [HttpGet, Route("avarage")]
    public virtual async Task<IActionResult> GetAvarage(Filter<TEntity> filter, CancellationToken token)
    {
        return await FromService((IReadRepository<TEntity, TKey> repo) => repo.GetAvarage(filter, token));
    }

    [HttpPut, Route("{id}")]
    public virtual async Task<IActionResult> CreateOne(TKey id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await FromService((ICreateRepository<TEntity, TKey> repo) => repo.Create(entity, token));
    }

    [HttpPut, Route("")]
    public virtual async Task<IActionResult> CreateMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((ICreateRepository<TEntity, TKey> repo) => repo.Create(entities, token));
    }

    [HttpPost, Route("{id}")]
    public virtual async Task<IActionResult> UpdateOne(TKey id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await FromService((IUpdateRepository<TEntity, TKey> repo) => repo.Update(entity));
    }

    [HttpPost, Route("")]
    public virtual async Task<IActionResult> UpdateMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((IUpdateRepository<TEntity, TKey> repo) => repo.Update(entities));
    }

    /// <summary>
    /// LIFO-cancels the latest scheduled (not-yet-active) version for the append-only valid-time entity in
    /// the body (only its business key is read), reopening the version it superseded. Returns 501 for entities
    /// that aren't append-only (no <see cref="IAppendOnlyTrackRepository{TEntity,TKey}"/> registered) and lets
    /// <see cref="BadRequestException"/> (nothing scheduled) surface as 400 via the central handler.
    /// </summary>
    [HttpPost, Route("cancel-pending")]
    public virtual async Task<IActionResult> CancelPending([FromBody] TEntity entity, CancellationToken token)
    {
        return await FromService((IAppendOnlyTrackRepository<TEntity, TKey> repo) => repo.CancelPending(entity, token));
    }

    [HttpPost, Route("upsert/{id}")]
    public virtual async Task<IActionResult> UpsertOne(TKey id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await FromService((ICreateRepository<TEntity, TKey> repo) => repo.UpsertAsync(entity, x => x.Id, token: token));
    }

    [HttpPost, Route("upsert")]
    public virtual async Task<IActionResult> UpsertMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((ICreateRepository<TEntity, TKey> repo) => repo.UpsertAsync(entities, x => x.Id, token: token));
    }

    [HttpPost, Route("merge/{id}")]
    public virtual async Task<IActionResult> MergeOne(TKey id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await FromService((ICreateRepository<TEntity, TKey> repo) => repo.MergeAsync(entity, x => x.Id, token: token));
    }

    [HttpPost, Route("merge")]
    public virtual async Task<IActionResult> MergeMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((ICreateRepository<TEntity, TKey> repo) => repo.MergeAsync(entities, x => x.Id, token: token));
    }

    [HttpPost, Route("get-or-create/{id}")]
    public virtual async Task<IActionResult> GetOrCreateOne(TKey id, [FromBody] TEntity entity, [FromQuery] string[]? key, Filter<TEntity> filter, CancellationToken token)
    {
        var keys = key?.Length > 0 ? key : [];
        return await FromService(async (ICreateRepository<TEntity, TKey> repo) =>
        {
            var result = await repo.GetOrCreateAsync([entity], keys, filter, token);
            return result.All;
        });
    }

    [HttpPost, Route("get-or-create")]
    public virtual async Task<IActionResult> GetOrCreateMany([FromBody] IEnumerable<TEntity> entities, [FromQuery] string[]? key, Filter<TEntity> filter, CancellationToken token)
    {
        var keys = key?.Length > 0 ? key : [];
        return await FromService(async (ICreateRepository<TEntity, TKey> repo) =>
        {
            var result = await repo.GetOrCreateAsync(entities, keys, filter, token);
            return result.All;
        });
    }

    [HttpPost, Route("remove")]
    public virtual async Task<IActionResult> Remove([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((IRemoveRepository<TEntity, TKey> repo) => repo.Remove(entities, token));
    }

    [HttpPost, Route("undo")]
    public virtual async Task<IActionResult> Undo([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((IRemoveRepository<TEntity, TKey> repo) => repo.Undo(entities, token));
    }

    [HttpDelete, Route("{id}")]
    public virtual async Task<IActionResult> Delete(TKey id, CancellationToken token)
    {
        return await FromService((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(id, token));
    }

    [HttpDelete, Route("")]
    public virtual async Task<IActionResult> Delete([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await FromService((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(entities, token));
    }

    [HttpDelete, Route("ids")]
    public virtual async Task<IActionResult> DeleteByIds([FromBody] IEnumerable<TKey> ids, CancellationToken token)
    {
        return await FromService((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(ids, token));
    }
}
