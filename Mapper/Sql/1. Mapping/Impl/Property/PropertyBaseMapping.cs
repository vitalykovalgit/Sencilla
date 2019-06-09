using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table;
using System.Data.Common;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Property
{
    public abstract class PropertyBaseMapping<TEntity, TProperty>: IPropertyMapping<TEntity> where TProperty : class, new()
    {
        /// <summary>
        /// 
        /// </summary>
        protected ITableMapping<TProperty> ForeignTableImpl;

        /// <summary>
        /// 
        /// </summary>
        protected ITableMappingCache TableMappingCache;

        /// <summary>
        /// 
        /// </summary>
        protected string JoinOnFieldName;

        protected PropertyBaseMapping(ITableMappingCache cache, IColumnMapping foreignKey, string joinOn, PropertyInfo info)
        {
            Name = info.Name;
            JoinOnFieldName = joinOn;
            ForeignKeyColumn = foreignKey;
            TableMappingCache = cache;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public IColumnMapping JoinOnColumn { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Foreign key column mapping
        /// </summary>
        public IColumnMapping ForeignKeyColumn { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// Property mapping
        /// </summary>
        ITableMapping IPropertyMapping.ForeignTable => ForeignTable;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public ITableMapping<TProperty> ForeignTable => ForeignTableImpl ?? InitForeignTable();

        /// <summary>
        /// 
        /// </summary>
        public abstract TEntity SetProperty(TEntity entity, DbDataReader reader, ReadSession session, int recursionDepth);

        /// <summary>
        /// 
        /// </summary>
        public abstract object GetProperty(TEntity entity);

        #region Implementation 

        private ITableMapping<TProperty> InitForeignTable()
        {
            if (ForeignTableImpl == null)
            {
                ForeignTableImpl = TableMappingCache.GetTableMapping<TProperty>();
                JoinOnColumn = string.IsNullOrEmpty(JoinOnFieldName)
                    ? ForeignTableImpl.PrimaryKey
                    : ForeignTableImpl.Columns.GetColumn(JoinOnFieldName);
            }

            return ForeignTableImpl;
        }

        #endregion 
    }
}
