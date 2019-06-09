using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table;
using System.Data.Common;

namespace Sencilla.Infrastructure.SqlMapper.Mapping
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPropertyMapping
    {
        /// <summary>
        /// Name of the navigation property 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Join on column mapping 
        /// </summary>
        IColumnMapping JoinOnColumn { get; }

        /// <summary>
        /// Foreign key column mapping
        /// </summary>
        IColumnMapping ForeignKeyColumn { get; }

        /// <summary>
        /// Property mapping table 
        /// </summary>
        ITableMapping ForeignTable { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Navigation property mapping
    /// </summary>
    public interface IPropertyMapping<TEntity> : IPropertyMapping
    {
        /// <summary>
        /// Set data from data reader to entity property 
        /// </summary>
        /// <param name="entity"> instance of the entity </param>
        /// <param name="reader"> data reader </param>
        /// <returns> the same instance as was passed to the entity param. (To make builder pattern)</returns>
        TEntity SetProperty(TEntity entity, DbDataReader reader, ReadSession session, int recursionDepth);

        /// <summary>
        /// Get field as a string 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        object GetProperty(TEntity entity);
    }
}
