using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Sencilla.Core.Entity;
using Sencilla.Core.Repo;
using Sencilla.Infrastructure.SqlMapper.Impl;

namespace Sencilla.Impl.Repository.SqlMapper
{
    public class CachedReadRepository<TEntity, TContext, TKey> : BaseRepository<TContext>, IReadRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, new()
           where TContext : DbContext
    {
        private IReadRepository<TEntity, TKey> mReadRepo;
        private readonly CachePolicy mCachePoplicy;

        protected class CacheItem
        {
            public CacheItem(TEntity item)
            {
                Item = item;
                CreatedDate = DateTime.Now;
            }

            public TEntity Item { get; private set; }
            public DateTime CreatedDate { get; private set; }

            public static implicit operator TEntity(CacheItem item)
            {
                return item.Item;
            }
        }

        protected static object _lockObj = new object();
        protected static Dictionary<TKey, CacheItem> _cachedEntities = null;

        protected IReadRepository<TEntity, TKey> GetReadRepo()
        {
            return mReadRepo ?? (mReadRepo = Resolve<ReadRepository<TEntity, TContext, TKey>>());
        }

        public CachedReadRepository(CachePolicy cachePoplicy)
        {
            mCachePoplicy = cachePoplicy;
        }

        public void MakesureInitialized()
        {
            if (_cachedEntities == null)
            {
                lock (_lockObj)
                {
                    _cachedEntities = new Dictionary<TKey, CacheItem>();
                    if (mCachePoplicy.LoadAllData)
                        _cachedEntities = GetReadRepo().GetAll().ToDictionary(k => k.Id, v => new CacheItem(v));
                }
            }
        }

        public virtual List<TEntity> GetAll()
        {
            MakesureInitialized();

            return mCachePoplicy.LoadAllData
                ? _cachedEntities.Values.Select(e => (TEntity)e).ToList()
                : GetReadRepo().GetAll().ToList();
        }

        public virtual int GetCount()
        {
            MakesureInitialized();

            return mCachePoplicy.LoadAllData
                ? _cachedEntities.Count()
                : GetReadRepo().GetCount();
        }

        public virtual TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] includes)
        {
            MakesureInitialized();

            TEntity item = null;
            if (_cachedEntities.ContainsKey(id))
            {
                var cahedItem = _cachedEntities[id];
                item = cahedItem;

                if (mCachePoplicy.LifeDurationInMs != CachePolicy.Unlimited)
                {
                    var deathTime = cahedItem.CreatedDate.AddMilliseconds(mCachePoplicy.LifeDurationInMs);
                    if (deathTime < DateTime.Now)
                    {
                        item = GetReadRepo().GetById(id);
                        lock (_lockObj)
                        {
                            _cachedEntities.Remove(id);
                        }
                        AddToCache(item);
                    }
                }
            }
            else
            {
                item = GetReadRepo().GetById(id);
                AddToCache(item);
            }

            return item;
        }

        protected void AddToCache(TEntity item)
        {
            if (item != null)
            {
                lock (_lockObj)
                {
                    _cachedEntities[item.Id] = new CacheItem(item);
                }
            }
        }

        public Task<int> GetCountAsync(CancellationToken? token = null)
        {
            throw new NotImplementedException();
        }

        public List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<List<TEntity>> GetAllAsync(CancellationToken? token = null)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> GetByIdAsync(TKey id, CancellationToken token, params Expression<Func<TEntity, object>>[] includes)
        {
            throw new NotImplementedException();
        }

        public List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] includes)
        {
            throw new NotImplementedException();
        }
    }

    public class CachePolicy
    {
        public const int Unlimited = -1;

        /// <summary>
        /// -1 Unlimited
        /// </summary>
        public int MaxCacheItems { get; set; }

        /// <summary>
        /// -1 Unlimited
        /// </summary>
        public int LifeDurationInMs { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool LoadAllData { get; set; }
    }

    public class PersistentCache : CachePolicy
    {
        public PersistentCache()
        {
            LoadAllData = true;
            MaxCacheItems = Unlimited;
            LifeDurationInMs = Unlimited;
        }
    }

    public class PerRequestCache : CachePolicy
    {
        public PerRequestCache(int lifeDurationInMs)
        {
            LoadAllData = false;
            MaxCacheItems = Unlimited;
            LifeDurationInMs = lifeDurationInMs;
        }
    }
}
