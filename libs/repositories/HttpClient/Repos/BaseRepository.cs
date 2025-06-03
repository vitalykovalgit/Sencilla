using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Sencilla.Core;
using Sencilla.Core.Repo;

namespace Sencilla.Impl.Repository.HttpClient
{
    /// <summary>
    /// Base repository for mobile requests 
    /// </summary>
    public abstract class BaseRepository
    {
        
        public BaseRepository(IResolver resolver)
        {
            Resolver = resolver;
        }

        protected IResolver Resolver { get; set; }

        protected TType R<TType>()
        {
            return Resolver.Resolve<TType>();
        }

        /// <summary>
        /// 
        /// </summary>
        protected readonly System.Net.Http.HttpClient Client = new System.Net.Http.HttpClient();

        public async Task<TEntity> GetAsync<TEntity>(string url, CancellationToken? token = null) 
        {            
            var response = await Client.GetAsync(url, token ?? CancellationToken.None).ConfigureAwait(false);
            response = Validate(response);
            return await ContentToEntity<TEntity>(response).ConfigureAwait(false);
        }

        public async Task<TEntity> PostAsync<TEntity>(string url, HttpContent content, CancellationToken? token = null) where TEntity : class, new()
        {
            var response = await Client.PostAsync(url, content, token ?? CancellationToken.None).ConfigureAwait(false);
            response = Validate(response);
            return await ContentToEntity<TEntity>(response).ConfigureAwait(false);
        }

        public async Task<TEntity> PutAsync<TEntity>(string url, HttpContent content, CancellationToken? token = null) where TEntity : class, new()
        {
            //var log = Logger.Debug(Tag, $"Put() {url}...");
            //if (CurrentUser != null)
            //    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentUser.Token);

            var response = await Client.PutAsync(url, content, token ?? CancellationToken.None).ConfigureAwait(false);
            response = Validate(response);
            return await ContentToEntity<TEntity>(response).ConfigureAwait(false);
        }

        public async Task<TEntity> DeleteAsync<TEntity>(string url, CancellationToken? token = null) where TEntity : class, new()
        {
            var response = await Client.DeleteAsync(url, token ?? CancellationToken.None).ConfigureAwait(false);
            response = Validate(response);
            return await ContentToEntity<TEntity>(response).ConfigureAwait(false);
        }

        #region Public Methods 

        /// <summary>
        /// Get query string from object 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected string GetQueryString(object obj)
        {
            if (obj == null)
                return string.Empty;

            var props = new List<string>();
            foreach (var p in obj.GetType().GetProperties())
            {
                if (!p.GetCustomAttributes(typeof(SkipInUrlParamsAttribute), false).Any())
                {
                    var value = p.GetValue(obj, null);
                    if (value != null)
                        props.Add(p.Name + "=" + WebUtility.UrlEncode(value.ToString()));
                }
            }

            return string.Join("&", props.ToArray());
        }

        /// <summary>
        /// Convert entity to json content 
        /// </summary>
        protected HttpContent ToJsonContent<TEntity>(TEntity entity) where TEntity : class, new()
        {
            var content = JsonConvert.SerializeObject(entity);
            return new StringContent(content, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Convert content to entity 
        /// </summary>
        protected async Task<TEntity> ContentToEntity<TEntity>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TEntity>(content);
        }

        protected HttpResponseMessage Validate(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedException();
                case HttpStatusCode.Forbidden:
                    //throw new ForbiddenException(await ContentToEntity<ServerError>(response));
                    throw new ForbiddenException();
                case HttpStatusCode.InternalServerError:
                    throw new InternalServerErrorException();
                case HttpStatusCode.BadRequest:
                    //throw new BadRequestException(await ContentToEntity<ServerError>(response));
                    throw new BadRequestException();
                default:
                    {
                        if (!response.IsSuccessStatusCode)
                            throw new SencillaException();

                        return response;
                    }
            }
        }

        #endregion 
    }
}
