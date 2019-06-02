using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table;
using System;
using System.Data.Common;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Property
{
    public class PropertyMapping<TEntity, TProperty> : PropertyBaseMapping<TEntity, TProperty> where TProperty  : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        protected Func<TEntity, TProperty> Getter;

        /// <summary>
        /// 
        /// </summary>
        protected Action<TEntity, TProperty> Setter;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="foreignKey"></param>
        /// <param name="info"></param>
        /// <param name="name"> Property name </param>
        public PropertyMapping(ITableMappingCache cache, IColumnMapping foreignKey, string joinOn, PropertyInfo info)
            : base (cache, foreignKey, joinOn, info)
        {
            Getter = (Func<TEntity, TProperty>)Delegate.CreateDelegate(typeof(Func<TEntity, TProperty>), info.GetGetMethod());
            Setter = (Action<TEntity, TProperty>)Delegate.CreateDelegate(typeof(Action<TEntity, TProperty>), info.GetSetMethod());
        }


        /// <inheritdoc />
        /// <summary>
        /// Set data from data reader to entity property 
        /// </summary>
        /// <param name="entity"> instance of the entity </param>
        /// <param name="reader"> data reader </param>
        /// <returns> the same instance as was passed to the entity param. (To make builder pattern)</returns>
        public override TEntity SetProperty(TEntity entity, DbDataReader reader, ReadSession session, int recursionDepth)
        {
            if (entity != null)
            {
                var previousProperty = Getter(entity);
                var currentProperty = ForeignTable.ToEntity(reader, previousProperty, session, ++recursionDepth);
                Setter(entity, currentProperty);
            }
            return entity;
        }

        /// <inheritdoc />
        /// <summary>
        /// Get field as an object
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override object GetProperty(TEntity entity)
        {
            return entity == null ? null : Getter(entity);
        }
    }
}
