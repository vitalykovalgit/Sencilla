using System;

namespace Sencilla.Core.Repo
{
    public abstract class WebContext
    {
        public WebContext()
        { 
        }

        public virtual string Server { get; protected set; }

        public virtual string GetAllPath<TEntity>()
        {
            return EntityToApiPath<TEntity>();
        }

        public virtual string GetCountPath<TEntity>()
        {
            return $"{EntityToApiPath<TEntity>()}/count";
        }

        public virtual string GetByIdPath<TEntity, TKey>(TKey key)
        {
            return $"{EntityToApiPath<TEntity>()}/{key}";
        }

        public virtual string GetUpdatePath<TEntity, TKey>(TKey key)
        {
            return $"{EntityToApiPath<TEntity>()}";
        }

        public virtual string GetDeletePath<TEntity, TKey>(TKey key)
        {
            return $"{EntityToApiPath<TEntity>()}";
        }

        public virtual string GetRemovePath<TEntity, TKey>(TKey key)
        {
            return $"{EntityToApiPath<TEntity>()}";
        }

        public string EntityToApiPath<TEntity>()
        {
            var urlAttr = typeof(TEntity).GetCustomAttributes(typeof(ApiUrlAttribute), true);
            if (urlAttr == null || urlAttr.Length == 0)
                throw new InvalidOperationException($"The type [{typeof(TEntity).FullName}] must have attribute [{nameof(ApiUrlAttribute)}] in order to use in repository");

            var apiUrl = urlAttr[0] as ApiUrlAttribute;
            return $"{Server}/{apiUrl.Url}";
        }
    }
}
