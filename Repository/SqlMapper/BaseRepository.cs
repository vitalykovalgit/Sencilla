﻿using Sencilla.Core.Injection;
using Sencilla.Core.Repo;
using Sencilla.Infrastructure.SqlMapper.Impl;

namespace Sencilla.Impl.Repository.SqlMapper
{
    public class BaseRepository<TContext> : IBaseRepository
           where TContext : DbContext
    {
        public BaseRepository(IResolver resolver)
        {
            Resolver = resolver;
        }

        /// <summary>
        /// Container to resolve context, please use resolve instead 
        /// </summary>
        protected IResolver Resolver { get; set; }

        /// <summary>
        /// Context implementation
        /// </summary>
        protected TContext ContextImpl { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            ContextImpl?.Commit();
        }

        /// <summary>
        /// Encapsulates work with Unity container 
        /// </summary>
        /// <typeparam name="T"> Type for which instance will be created </typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return Resolver.Resolve<T>();
        }
    }
}
