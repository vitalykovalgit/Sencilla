using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Property
{
    public class PropertyListMapping<TEntity, TProperty> : PropertyBaseMapping<TEntity, TProperty> where TProperty  : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        protected Func<TEntity, ICollection<TProperty>> Getter;

        /// <summary>
        /// 
        /// </summary>
        protected Action<TEntity, ICollection<TProperty>> Setter;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="foreignKey"></param>
        /// <param name="info"></param>
        /// <param name="name"> Property name </param>
        public PropertyListMapping(ITableMappingCache cache, IColumnMapping foreignKey, string joinOn, PropertyInfo info)
            : base (cache, foreignKey, joinOn, info)
        {
            try
            {
                Getter = (Func<TEntity, ICollection<TProperty>>)Delegate.CreateDelegate(typeof(Func<TEntity, ICollection<TProperty>>), info.GetGetMethod());
                Setter = (Action<TEntity, ICollection<TProperty>>)Delegate.CreateDelegate(typeof(Action<TEntity, ICollection<TProperty>>), info.GetSetMethod());
            }
            catch (Exception)
            {
                // Hide exception here, the result will be checked later 
            }
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
                // Get collection, if not exists create it 
                var collection = Getter(entity);
                if (collection == null)
                {
                    if (Setter != null)
                        Setter(entity, (collection = new List<TProperty>()));
                    else
                        throw new InvalidOperationException($"Please make sure that navigation property {typeof(TProperty)} collection is initialized or has ICollection<TProperty> signature");
                }

                // Get previous entity 
                // Check collection is List for optimized access of previous entity
                var previousProperty = default(TProperty);
                if (typeof(List<TProperty>) == collection.GetType())
                {
                    var pkIdx = ForeignTable.PrimaryKey.GetColumnIndexByAlias(reader);
                    if (pkIdx.HasValue)
                    {
                        // TODO: Think about more effective way of reading previous entity 
                        if (!ForeignTable.PrimaryKey.IsNull(reader, pkIdx.Value))
                        {
                            var pkValue = ForeignTable.PrimaryKey.GetValue(reader, /*shift*/ pkIdx.Value - ForeignTable.PrimaryKey.Index.Value);
                            previousProperty = collection.FirstOrDefault(e => ForeignTable.PrimaryKey.GetField(e).Equals(pkValue));
                        }
                    }

                    //if (collection.Count > 0)
                    //    previousProperty = ((List<TProperty>)collection)[collection.Count - 1];
                }
                else
                {
                    // Try to find entity with the same key in collection 
                    var pkIdx = ForeignTable.PrimaryKey.GetColumnIndexByAlias(reader);
                    if (pkIdx.HasValue)
                    {
                        // TODO: Think about more effective way of reading previous entity 
                        if (!ForeignTable.PrimaryKey.IsNull(reader, pkIdx.Value))
                        {
                            var pkValue = ForeignTable.PrimaryKey.GetValue(reader, /*shift*/ pkIdx.Value - ForeignTable.PrimaryKey.Index.Value);
                            previousProperty = collection.FirstOrDefault(e => ForeignTable.PrimaryKey.GetField(e).Equals(pkValue));
                        }
                    }
                }

                var propertyInstance = ForeignTable.ToEntity(reader, previousProperty, session, ++recursionDepth);
                if (propertyInstance != null && propertyInstance != previousProperty) 
                    collection?.Add(propertyInstance);
            }
            return entity;
        }

        /// <inheritdoc />
        /// <summary>
        /// Get field as a string 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override object GetProperty(TEntity entity)
        {
            return entity == null ? null : Getter(entity);
        }
    }
}
