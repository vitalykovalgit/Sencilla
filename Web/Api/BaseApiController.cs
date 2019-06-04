using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Sencilla.Core.Injection;
using Sencilla.Core.Logging;
using Sencilla.Core.Web;

namespace Sencilla.Web.Api
{
    public abstract class ApiController : ControllerBase
    {
        public ILogger Logger { get; set; }

        public IResolver Resolver { get; set; }

        #region Custom Statuses
        
        /// <summary>
        /// Returns response with status forbidden
        /// </summary>
        /// <returns></returns>
        public IActionResult Forbidden(string msg = null)
        {
            // "You are not authorized to access this item");
            return base.Forbid();
        }

        /// <summary>
        /// Returns No Content response
        /// </summary>
        /// <returns></returns>
        public IActionResult NoContent(string msg = null)
        {
            return base.NoContent();
        }

        /// <summary>
        /// Returns not implemented response
        /// </summary>
        /// <returns></returns>
        public IActionResult NotImplemented(string msg = null)
        {
            return CustomResult(HttpStatusCode.NotImplemented, msg);
        }

        /// <summary>
        /// Returns not implemented response
        /// </summary>
        /// <returns></returns>
        public IActionResult NotFound(string msg = null)
        {
            return base.NotFound(msg);
        }

        /// <summary>
        /// Already deleted
        /// </summary>
        /// <returns></returns>
        public IActionResult ResourceGone(string msg = null)
        {
            return CustomResult(HttpStatusCode.Gone, msg);
        }

        private IActionResult CustomResult(HttpStatusCode code, string msg = null)
        {
            return new ObjectResult(msg) { StatusCode = (int)code };
        }

        #endregion

        #region Handling Requests

        protected IActionResult ExceptionToResponse(Exception ex)
        {
            //if (ex is EntityNotExistException)
            //    return NotFound(ex.Message);

            //if (ex is EntityDeletedException)
            //    return ResourceGone(ex.Message);

            //if (ex is EntityAccessForbiddenException)
            //    return Forbidden(ex.Message);

            return CustomResult(HttpStatusCode.InternalServerError, $"{ex.Message}\r\n {ex.StackTrace}");
        }

        /// <summary>
        /// Handle simple AJAX request which return OK
        /// if 'handler' parameter action doesn't throw exception
        /// </summary>
        /// <param name="handler"> Action that will be preformed </param>
        /// <returns> Internal server error if handler throws exception otherwise OK </returns>
        protected IActionResult HandleAjaxCall(Action handler)
        {
            try
            {
                handler();
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        /// <summary>
        /// Handle simple AJAX request 
        /// if 'handler' parameter action doesn't throw exception
        /// </summary>
        /// <param name="handler"> Action that will be preformed </param>
        /// <returns> Internal server error if handler throws exception otherwise OK </returns>
        protected IActionResult HandleAjaxCallCustom(Func<IActionResult> handler)
        {
            try
            {
                return handler();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        /// <summary>
        /// Handle simple AJAX request 
        /// if 'handler' parameter action doesn't throw exception
        /// </summary>
        /// <param name="handler"> Action that will be preformed </param>
        /// <returns> Internal server error if handler throws exception otherwise OK </returns>
        protected IActionResult HandleAjaxJsonCall<T>(Func<T> handler)
        {
            try
            {
                //return Json(handler(), new JsonSerializerSettings
                //{
                //    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                //    NullValueHandling = NullValueHandling.Ignore
                //});
                return Ok(handler());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        /// <summary>
        /// Handle simple AJAX request which return OK
        /// if 'handler' parameter action doesn't throw exception
        /// </summary>
        /// <param name="handler"> Action that will be preformed </param>
        /// <returns> Internal server error if handler throws exception otherwise OK </returns>
        public async Task<IActionResult> HandleAjaxCallAsync(Func<Task> handler)
        {
            try
            {
                await handler();
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        /// <summary>
        ///8
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        protected IActionResult HandleAjaxCall<T>(Func<T> handler)
        {
            try
            {
                var entity = handler();
                return entity == null ? NotFound() : Ok(entity);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task<IActionResult> HandleAjaxCallAsync<T>(Func<Task<T>> handler)
        {
            try
            {
                var entity = await handler();
                return entity == null ? NotFound() : Ok(entity);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        ///  <summary>
        /// 
        ///  </summary>
        ///  <typeparam name="TWebEntity"></typeparam>
        ///  <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="entityHandler"></param>
        ///  <returns></returns>
        protected IActionResult HandleAjaxCall<TWebEntity, TEntity, TKey>(Func<TEntity> entityHandler)
            where TWebEntity : IWebEntity<TEntity, TKey>, new()
            where TEntity : class, new()
        {
            try
            {
                var entity = entityHandler();
                if (entity == null)
                    return NotFound();

                var webEntity = new TWebEntity();
                webEntity.FromEntity(entity);
                return Ok(webEntity);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        ///  <summary>
        /// 
        ///  </summary>
        ///  <typeparam name="TWebEntity"></typeparam>
        ///  <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="entityHandler"></param>
        ///  <returns></returns>
        protected async Task<IActionResult> HandleAjaxCallAsync<TWebEntity, TEntity, TKey>(Func<Task<TEntity>> entityHandler)
            where TWebEntity : IWebEntity<TEntity, TKey>, new()
            where TEntity : class, new()
        {
            try
            {
                var entity = await entityHandler();
                if (entity == null)
                    return NotFound();

                var webEntity = new TWebEntity();
                webEntity.FromEntity(entity);
                return Ok(webEntity);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        ///  <summary>
        /// 
        ///  </summary>
        ///  <typeparam name="TWebEntity"></typeparam>
        ///  <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="handler"></param>
        ///  <returns></returns>
        protected IActionResult HandleAjaxCall<TWebEntity, TEntity, TKey>(Func<IEnumerable<TEntity>> handler)
            where TWebEntity : IWebEntity<TEntity, TKey>, new()
            where TEntity : class, new()
        {
            try
            {
                var entities = handler();
                if (entities == null)
                    return NotFound();

                var webEntities = entities.Select(entity =>
                {
                    var webEntity = new TWebEntity();
                    webEntity.FromEntity(entity);

                    return webEntity;
                });
                return Ok(webEntities);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        ///  <summary>
        /// 
        ///  </summary>
        ///  <typeparam name="TWebEntity"></typeparam>
        ///  <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="handler"></param>
        ///  <returns></returns>
        protected async Task<IActionResult> HandleAjaxCallAsync<TWebEntity, TEntity, TKey>(Func<Task<IEnumerable<TEntity>>> handler)
            where TWebEntity : IWebEntity<TEntity, TKey>, new()
            where TEntity : class, new()
        {
            try
            {
                var entities = await handler();
                if (entities == null)
                    return NotFound();

                var webEntities = entities.Select(entity =>
                {
                    var webEntity = new TWebEntity();
                    webEntity.FromEntity(entity);

                    return webEntity;
                });
                return Ok(webEntities);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ExceptionToResponse(ex);
            }
        }

        #endregion
    }
}
