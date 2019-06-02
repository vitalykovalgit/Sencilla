
using System;
using System.Collections.Concurrent;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table
{
    public class TableMappingCache : ITableMappingCache
    {
        private static readonly ConcurrentDictionary<Type, ITableMapping> Cache = new ConcurrentDictionary<Type, ITableMapping>();

        public static TableMappingCache Instance { get; }

        static TableMappingCache()
        {
            Instance = new TableMappingCache();
        }

        public ITableMapping<TEntity> GetTableMapping<TEntity>() where TEntity : class, new()
        {
            var type = typeof(TEntity);
            if (!Cache.ContainsKey(type))
                Cache.TryAdd(type, new TableMapping<TEntity>());

            return Cache[type] as ITableMapping<TEntity>;
        }


    }
}
