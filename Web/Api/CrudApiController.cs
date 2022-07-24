//using System;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Collections.Generic;

//using Microsoft.AspNetCore.Mvc;
//using Sencilla.Core;

//namespace Sencilla.Web.Api
//{
//    public class EntityCrudApiController<TEntity, TWebEntity, TKey> : ApiController
//           where TEntity : class, IEntity<TKey>, new()
//           where TWebEntity : IDtoEntity<TEntity, TKey>, new()
//    {
//        public EntityCrudApiController(ILogger logger, IResolver resolver) : base(logger, resolver)
//        {
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        protected virtual Expression<Func<TEntity, object>>[] Includes => new Expression<Func<TEntity, object>>[0];
        
//        /// <summary>
//        /// Returns all entities in the table.
//        /// </summary>
//        /// <returns></returns>
//        [HttpGet, Route("")]
//        public virtual IActionResult GetAll()
//        {
//            var repoRead = Resolver.Resolve<IReadRepository<TEntity, TKey>>();
//            if (repoRead == null)
//                return NotImplemented();

//            //TODO: Check access
//            //if (!CheckAccessToPage(pageId, Permission.Read))
//            //	return Forbidden();

//            return HandleAjaxCall(() =>
//            {
//                return repoRead.GetAll(Includes).Select(e =>
//                {
//                    var we = new TWebEntity(); 
//                    we.FromEntity(e);
//                    return we;

//                }).ToList();
//            });
//        }

//        /// <summary>
//        /// Get information from IReadRepository by id
//        /// </summary>
//        /// <param name="id">Id of the TEntity</param>
//        /// <returns>TEntity</returns>
//        [HttpGet, Route("{id}")]
//        public virtual IActionResult GetById(TKey id)
//        {
//            var repoRead = Resolver.Resolve<IReadRepository<TEntity, TKey>>();
//            if (repoRead == null)
//                return NotImplemented();

//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() =>
//            {
//                var entity = repoRead.GetById(id, Includes);
//                if ((entity as IEntityRemoveable<TKey>)?.DeletedDate != null)
//                    return null;

//                return entity;
//            });
//        }

//        [HttpGet, Route("count")]
//        public virtual IActionResult GetCount()
//        {
//            var repoRead = Resolver.Resolve<IReadRepository<TEntity, TKey>>();
//            if (repoRead == null)
//                return NotImplemented();

//            return HandleAjaxCall(() =>
//            {
//                return repoRead.GetCount();
//            });
//        }

//        /// <summary>
//        /// Creates Web Entity
//        /// </summary>
//        /// <param name="entity">entity</param>
//        /// <returns></returns>
//        [HttpPut, Route("")]
//        public virtual IActionResult Create([FromBody] TWebEntity entity)
//        {
//            if (entity == null)
//                // TODO: Remove text from code
//                return BadRequest("Web entity is empty");

//            var repoCreate = Resolver.Resolve<ICreateRepository<TEntity, TKey>>();
//            if (repoCreate == null)
//                return NotImplemented();

//            var dbEntity = entity.ToEntity(new TEntity());
//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() => repoCreate.Create(dbEntity));
//        }

//        /// <summary>
//        /// Creates Web Entity
//        /// </summary>
//        /// <param name="entity">entity</param>
//        /// <returns></returns>
//        [HttpPut, Route("multiple")]
//        public virtual IActionResult CreateMany([FromBody] IEnumerable<TWebEntity> entities)
//        {
//            if (entities == null || entities.Count() == default)
//            {
//                return BadRequest("Web entities are null or empty");
//            }

//            var repoCreate = Resolver.Resolve<ICreateRepository<TEntity, TKey>>();
//            if (repoCreate == null)
//                return NotImplemented();

//            List<TEntity> dbEntities = new List<TEntity>();
//            entities.ToList().ForEach(e => dbEntities.Add(e.ToEntity(new TEntity())));

//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() => repoCreate.Create(dbEntities));
//        }


//        /// <summary>
//        /// Update Web entity
//        /// </summary>
//        /// <param name="entity">Web entity</param>
//        /// <returns>TWebEntity</returns>
//        [HttpPost, Route("")]
//        public virtual IActionResult Update(TWebEntity entity)
//        {
//            var repoUpdate = Resolver.Resolve<IUpdateRepository<TEntity, TKey>>();
//            if (repoUpdate == null)
//                return NotImplemented();

//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() =>
//            {
//                var dbEntity = repoUpdate.GetById(entity.Id);
//                return dbEntity == null ? null : repoUpdate.Update(entity.ToEntity(dbEntity));
//            });
//        }

//        /// <summary>
//        /// Remove TEntity by id
//        /// </summary>
//        /// <param name="id">id</param>
//        /// <returns></returns>
//        [HttpPost, Route("remove")]
//        public virtual IActionResult Remove([FromBody] TKey id)
//        {
//            var repoRemove = Resolver.Resolve<IRemoveRepository<TEntity, TKey>>();
//            if (repoRemove == null)
//                return NotImplemented();

//            // TODO: Check access 

//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() => repoRemove.Remove(id));
//        }

//        /// <summary>
//        /// Undo TEntity by id
//        /// </summary>
//        /// <param name="id">TEntity id</param>
//        /// <returns></returns>
//        [HttpPost, Route("undo")]
//        public virtual IActionResult Undo([FromBody] TKey id)
//        {
//            var repoRemove = Resolver.Resolve<IRemoveRepository<TEntity, TKey>>();
//            if (repoRemove == null)
//                return NotImplemented();

//            // TODO: Check access 
            
//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() => repoRemove.Undo(id));
//        }

//        /// <summary>
//        /// Delete TEntity by id
//        /// </summary>
//        /// <param name="id">TEntity id</param>
//        /// <returns></returns>
//        [HttpDelete, Route("{id}")]
//        public virtual IActionResult Delete(TKey id)
//        {
//            var repoDelete = Resolver.Resolve<IDeleteRepository<TEntity, TKey>>();
//            if (repoDelete == null)
//                return NotImplemented();

//            return HandleAjaxCall<TWebEntity, TEntity, TKey>(() => {
//                    repoDelete.Delete(id);
//                    return new TEntity();
//                });
//        }

//    }
//}
