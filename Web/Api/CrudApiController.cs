using Sencilla.Core;

namespace Microsoft.AspNetCore.Mvc
{
    public class CrudApiController<TEntity, TKey> : ApiController where TEntity : class, IEntity<TKey>, new()
    {
        public CrudApiController(IResolver resolver) : base(resolver)
        {
        }

        [HttpGet, Route("")]
        public virtual async Task<IActionResult> GetAll(Filter filter, CancellationToken token)
        {
            return await AjaxAction((IReadRepository<TEntity, TKey> repo) => repo.GetAll(filter, token));
        }

        [HttpGet, Route("{id}")]
        public virtual async Task<IActionResult> GetById(TKey id, Filter filter)
        {
            return await AjaxAction((IReadRepository<TEntity, TKey> repo) => repo.GetById(id));
        }

        [HttpGet, Route("count")]
        public virtual async Task<IActionResult> GetCount(Filter filter, CancellationToken token)
        {
            return await AjaxAction((IReadRepository<TEntity, TKey> repo) => repo.GetCount(filter, token));
        }

        [HttpPut, Route("")]
        public virtual async Task<IActionResult> Create([FromBody] TEntity entity, CancellationToken token)
        {
            return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.Create(entity, token));
        }

        [HttpPut, Route("multiple")]
        public virtual async Task<IActionResult> CreateMany([FromBody] IEnumerable<TEntity> entities, CancellationToken token)
        {
            return await AjaxAction((ICreateRepository<TEntity, TKey> repo) => repo.Create(entities, token));
        }

        [HttpPost, Route("")]
        public virtual async Task<IActionResult> Update(TEntity entity, CancellationToken token)
        {
            return await AjaxAction((IUpdateRepository<TEntity, TKey> repo) => repo.Update(entity));
        }

        [HttpPost, Route("remove")]
        public virtual async Task<IActionResult> Remove([FromBody] TEntity entity)
        {
            return await AjaxAction((IRemoveRepository<TEntity, TKey> repo) => repo.Remove(entity));
        }

        [HttpPost, Route("undo")]
        public virtual async Task<IActionResult> Undo([FromBody] TEntity entity)
        {
            return await AjaxAction((IRemoveRepository<TEntity, TKey> repo) => repo.Undo(entity));
        }

        [HttpDelete, Route("{id}")]
        public virtual async Task<IActionResult> Delete(TKey id)
        {
            return await AjaxAction((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(id));
        }

        [HttpDelete, Route("")]
        public virtual async Task<IActionResult> Delete(TEntity entity)
        {
            return await AjaxAction((IDeleteRepository<TEntity, TKey> repo) => repo.Delete(entity));
        }

    }
}
