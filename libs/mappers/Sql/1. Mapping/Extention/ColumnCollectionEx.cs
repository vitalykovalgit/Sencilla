using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Extention
{
    /// <summary>
    /// 
    /// </summary>
    public static class ColumnCollectionEx
    {
        /// <summary>
        /// Convert columns to SqlParamater collection with values 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="columns"></param>
        /// <param name="factory"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static List<DbParameter> ToParams<TEntity>(this IColumnCollection<TEntity> columns, IDbProviderParam factory, TEntity entity)
        {
            return columns.ToParams(column => true, factory, entity);
        }

        /// <summary>
        /// Convert columns to SqlParamater collection with values 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="columns"></param>
        /// <param name="factory"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static List<DbParameter> ToParams<TEntity>(this IColumnCollection<TEntity> columns, Func<IColumnMapping<TEntity>, bool> filter, IDbProviderParam factory, TEntity entity)
        {
            var parameters = new List<DbParameter>();

            foreach (var c in columns)
            {
                if (filter(c))
                {
                    var value = c.GetField(entity);
                    parameters.Add(factory.Create(c.Param, value, c.FieldType));
                }
            }

            return parameters;
        }

        /// <summary>
        /// This function must be used with ToParams
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="columns"></param>
        /// <param name="entity"></param>
        /// <param name="parameters"> Parameters returned by ToParams method </param>
        /// <returns></returns>
        public static List<DbParameter> UpdateParams<TEntity>(this IColumnCollection<TEntity> columns, TEntity entity, List<DbParameter> parameters)
        {
            foreach (var entry in columns.Zip(parameters, (c, p)=> new { Column = c, Paramater = p }))
                entry.Paramater.Value = entry.Column.GetField(entity);

            return parameters;
        }
    }
}
