﻿namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class CrudApiController<TEntity> : CrudApiController<TEntity, int> where TEntity : class, IEntity<int>, new()
{
    public CrudApiController(IResolver resolver) : base(resolver)
    {
    }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class CrudApiController<TEntity, TKey> : ApiController where TEntity : class, IEntity<TKey>, new()
{
    public CrudApiController(IResolver resolver) : base(resolver)
    {
    }

    [HttpGet, Route("")]
    public virtual async Task<IActionResult> GetAll(Filter<TEntity> filter, CancellationToken token)
    {
        return await AjaxAction((IReadRepository<TEntity, TKey> repo) => repo.GetAll(filter, token));
    }

    [HttpGet, Route("{id}")]
    public virtual async Task<IActionResult> GetById(TKey id, Filter<TEntity> filter)
    {
        return await AjaxAction((IReadRepository<TEntity, TKey> repo) => repo.GetById(id));
    }

    [HttpGet, Route("count")]
    public virtual async Task<IActionResult> GetCount(Filter<TEntity> filter, CancellationToken token)
    {
        return await AjaxAction((IReadRepository<TEntity, TKey> repo) => repo.GetCount(filter, token));
    }

    [HttpPut, Route("{id}")]
    public virtual async Task<IActionResult> CreateOne(int id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.Create(entity, token));
    }

    [HttpPut, Route("")]
    public virtual async Task<IActionResult> CreateMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.Create(entities, token));
    }

    [HttpPost, Route("{id}")]
    public virtual async Task<IActionResult> UpdateOne(int id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await AjaxAction((IUpdateRepository<TEntity, TKey> repo) => repo.Update(entity));
    }

    [HttpPost, Route("")]
    public virtual async Task<IActionResult> UpdateMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((IUpdateRepository<TEntity, TKey> repo) => repo.Update(entities));
    }

    [HttpPost, Route("upsert/{id}")]
    public virtual async Task<IActionResult> UpsertOne(int id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.UpsertAsync(entity, x => x.Id, token: token));
    }

    [HttpPost, Route("upsert")]
    public virtual async Task<IActionResult> UpsertMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.UpsertAsync(entities, x => x.Id, token: token));
    }

    [HttpPost, Route("merge/{id}")]
    public virtual async Task<IActionResult> MergeOne(int id, [FromBody] TEntity entity, CancellationToken token)
    {
        return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.MergeAsync(entity, x => x.Id, token: token));
    }

    [HttpPost, Route("merge")]
    public virtual async Task<IActionResult> MergeMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.MergeAsync(entities, x => x.Id, token: token));
    }

    [HttpPost, Route("remove")]
    public virtual async Task<IActionResult> Remove([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((IRemoveRepository<TEntity, TKey> repo) => repo.Remove(entities, token));
    }

    [HttpPost, Route("undo")]
    public virtual async Task<IActionResult> Undo([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((IRemoveRepository<TEntity, TKey> repo) => repo.Undo(entities, token));
    }

    [HttpDelete, Route("{id}")]
    public virtual async Task<IActionResult> Delete(TKey id, CancellationToken token)
    {
        return await AjaxAction((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(id, token));
    }

    [HttpDelete, Route("")]
    public virtual async Task<IActionResult> Delete([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
    {
        return await AjaxAction((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(entities, token));
    }

    [HttpDelete, Route("ids")]
    public virtual async Task<IActionResult> DeleteByIds([FromBody] IEnumerable<TKey> ids, CancellationToken token)
    {
        return await AjaxAction((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(ids, token));
    }
}
