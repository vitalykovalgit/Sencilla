using Sencilla.Infrastructure.SqlMapper.Impl;
using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column;
using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Property;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table
{
    public partial class TableMapping<TEntity> : ITableMapping<TEntity> where TEntity : class, new()
    {
        private const int MaxRecursionDepth = 5;

        #region Private Fields 

        protected Settings Settings => Settings.Instance;

        private static string mName;
        private static string mSafeName;
        private static string mAlias;
        private static string mSchema;
        private static string mReference;

        /// <summary>
        /// All columns 
        /// </summary>
        private readonly ColumnCollection<TEntity> mColumns = new ColumnCollection<TEntity>();

        /// <summary>
        /// Columns that can be written to database
        /// </summary>
        private readonly ColumnCollection<TEntity> mWriteColumns = new ColumnCollection<TEntity>();

        /// <summary>
        /// One to one relationship properties 
        /// </summary>
        private readonly PropertyCollection<TEntity> mProperties = new PropertyCollection<TEntity>();

        #endregion

        /// <summary>
        /// Initialize table mapping 
        /// </summary>
        public TableMapping()
        {
            var entityType = typeof(TEntity);
            GetTableNameAndSchema(entityType);
            InitColumnMapping(entityType);
        }

        /// <summary>
        /// Internally used for clone 
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="notUsed"></param>
        protected TableMapping(string alias, ColumnCollection<TEntity> notUsed)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Database table name which represents TEntity in DB
        /// </summary>
        public string Name => mName;

        public string SafeName => mSafeName;

        /// <inheritdoc />
        /// <summary>
        /// Database schema of the table 
        /// </summary>
        public string Schema => mSchema;

        /// <inheritdoc />
        /// <summary>
        /// Returns [Schema].[Name]
        /// </summary>
        public string Reference => mReference;

        /// <inheritdoc />
        /// <summary>
        /// Table alias 
        /// </summary>
        public string Alias => mAlias;

        /// <inheritdoc />
        /// <summary>
        /// Returns full declaration of table 
        /// e.g. [Schema].[Name] as [Alias]
        /// </summary>
        public string Declaration => string.IsNullOrWhiteSpace(Alias) ? Reference : $"{Reference} AS [{Alias}]";

        /// <inheritdoc />
        /// <summary>
        /// Return declaration 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Declaration;
        }

        /// <inheritdoc />
        /// <summary>
        /// Primary key column mapping. It returns null if there is no primary key 
        /// </summary>
        IColumnMapping ITableMapping.PrimaryKey => PrimaryKey;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        IColumnCollection ITableMapping.Columns => mColumns;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        IColumnCollection ITableMapping.WriteColumns => mWriteColumns;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        IPropertyCollection ITableMapping.Properties => mProperties;

        public IPropertyMapping GetProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var nl = name.ToLower();
            return mProperties.ContainsKey(nl) ? mProperties[nl] : null;
        }

        /// <inheritdoc />
        /// <summary>
        /// Primary key column mapping. It returns null if there is no primary key 
        /// </summary>
        public IColumnMapping<TEntity> PrimaryKey { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// List of columns 
        /// </summary>
        public IColumnCollection<TEntity> Columns => mColumns;

        /// <inheritdoc />
        /// <summary>
        /// Columns that can be written, (without readonly columns)
        /// </summary>
        public IColumnCollection<TEntity> WriteColumns => mWriteColumns;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public IPropertyCollection<TEntity> Properties => mProperties;

        public bool HasPrimaryKey(IDataReader reader)
        {
            return PrimaryKey?.IsColumnExist(reader) ?? false;
        }
        
        public TEntity ToEntity(DbDataReader reader, TEntity previous, ReadSession session, int recursionDepth = 0)
        {
            // If primary key properties is not exists or null return default value
            var primaryKeyIndex = PrimaryKey.GetColumnIndexByAlias(reader);
            if (primaryKeyIndex == null || reader.IsDBNull(primaryKeyIndex.Value))
                return default(TEntity);

            // Get current entity 
            var shift = (primaryKeyIndex.Value - PrimaryKey.Index) ?? 0;

            var isSameEntity = false;
            if (previous != null)
            {
                var prevKey = PrimaryKey.GetField(previous);
                var currKey = PrimaryKey.GetValue(reader, shift);
                isSameEntity = prevKey.Equals(currKey);
            }
            
            var entity = previous;
            if (!isSameEntity)
            {
                var positions = session.GetColumnPositions(typeof(TEntity), Columns.Count);

                // Set columns
                int idx = 0;
                entity = new TEntity();
                foreach (var column in Columns)
                {
                    var pos = positions[idx++];
                    column.SetField(entity, reader, pos);
                }
            }

            // Set one to one navigation properties
            if (recursionDepth < MaxRecursionDepth)
            {
                foreach (var property in Properties)
                    property.SetProperty(entity, reader, session, recursionDepth);
            }
                
            return entity;
        }

        public TEntity ReadFirstOrDefault(DbDataReader reader)
        {
            using (reader)
            {
                var session = new ReadSession();

                // Read entity 
                var entity = default(TEntity);
                var previous = entity;
                while (reader.Read())
                {
                    previous = entity;
                    entity = ToEntity(reader, previous, session);
                    if (previous != null && entity != previous)
                        break;
                }
                
                //// Read navigation properties 
                //while (reader.NextResult())
                //{
                //    // Find navigation property 
                //    var property = Properties.FirstOrDefault(p => p.ForeignTable.HasPrimaryKey(reader));
                //    property.SetProperty(, reader);
                //}
                
                return entity;
            }
        }

        public async Task<TEntity> ReadFirstOrDefaultAsync(DbDataReader reader, CancellationToken? token = null)
        {
            using (reader)
            {
                var session = new ReadSession();

                // Read entity 
                var entity = default(TEntity);
                var previous = entity;
                while (await reader.ReadAsync(token ?? CancellationToken.None))
                {
                    entity = ToEntity(reader, previous = entity, session);
                    if (entity != previous)
                        break;
                }
                
                return entity;
            }
        }

        public List<TEntity> ReadList(DbDataReader reader)
        {
            var list = new List<TEntity>();
            using (reader)
            {
                var session = new ReadSession();

                // Read entities 
                var entity = default(TEntity);
                var previous = entity;
                while (reader.Read())
                {
                    entity = ToEntity(reader, previous = entity, session);
                    if (entity != previous)
                        list.Add(entity);
                }
            }
            return list;
        }

        public async Task<List<TEntity>> ReadListAsync(DbDataReader reader, CancellationToken? token = null)
        {
            var list = new List<TEntity>();
            using (reader)
            {
                var session = new ReadSession();

                var entity = default(TEntity);
                var previous = entity;
                while (await reader.ReadAsync(token ?? CancellationToken.None))
                {
                    previous = entity;
                    entity = ToEntity(reader, previous, session);
                    if (entity != previous)
                        list.Add(entity);
                }
            }
            return list;
        }

        public TableMapping<TEntity> Clone(string alias)
        {
            var table = new TableMapping<TEntity>(alias, null);
            foreach (var column in Columns)
            {
                var clone = column.Clone(this);
                table.mColumns.Add(clone);

                if (!column.IsReadonly)
                    table.mWriteColumns.Add(clone);

                if (column == PrimaryKey)
                    table.PrimaryKey = clone;
            }
            
            return table;
        }

        #region Implementation 

        private void GetTableNameAndSchema(Type entityType)
        {
            // If name is already set, do nothing 
            if (!string.IsNullOrEmpty(mName))
                return;

            // Get table name and schema
            var attr = entityType.GetCustomAttribute<TableAttribute>();

            mName = string.IsNullOrWhiteSpace(attr?.Name) ? entityType.Name : attr.Name;
            mSchema = string.IsNullOrWhiteSpace(attr?.Schema) ? Settings?.DefaultSchema : attr.Schema;
            mSafeName = $"[{mName}]";
            mAlias = $"_{mSchema}{mName}_"; // TODO: think about shorter name 
            mReference = string.IsNullOrEmpty(mSchema) ? $"[{mName}]" : $"[{mSchema}].[{mName}]";
        }

        private void InitColumnMapping(Type entityType)
        {
            var navProps = new Dictionary<string, PropertyInfo>();
            var invProps = new List<KeyValuePair<string, PropertyInfo>>();

            // Create properties mappings
            var properties = entityType.GetProperties();
            foreach (var p in properties)
            {
                // Get field attributes 
                var columnKey = false;
                var fieldName = p.Name;
                var columnName = fieldName;
                var columnGeneratedByDb = (bool?)null;
                var columnIsRowVersion = false;

                var isNotMapped = false;
                foreach (var a in p.GetCustomAttributes(true))
                {
                    var attrType = a.GetType();
                    if (attrType == typeof(NotMappedAttribute))
                    {
                        isNotMapped = true;
                        break;
                    }
                    else if (attrType == typeof(ColumnAttribute))
                    {
                        columnName = (a as ColumnAttribute).Name;
                    }
                    else if (attrType == typeof(KeyAttribute))
                    {
                        columnKey = true;
                    }
                    else if (attrType == typeof(RowVersionAttribute))
                    {
                        columnIsRowVersion = true;
                    }
                    else if (attrType == typeof(DatabaseGeneratedAttribute))
                    {
                        columnGeneratedByDb = (a as DatabaseGeneratedAttribute).DatabaseGeneratedOption != DatabaseGeneratedOption.None;
                    }
                    else if (attrType == typeof(ForeignKeyAttribute))
                    {
                        navProps[(a as ForeignKeyAttribute).Name] = p; 
                    }
                    else if (attrType == typeof(InversePropertyCollectionAttribute))
                    {
                        invProps.Add(new KeyValuePair<string, PropertyInfo>((a as InversePropertyCollectionAttribute).Property, p));
                    }
                }

                if (!isNotMapped)
                {
                    var column = GetColumnMapping(p, columnName, fieldName);
                    if (column != null)
                    {
                        mColumns.Add(column);

                        column.IsRowVersion = columnIsRowVersion;
                        column.IsReadonly = (columnGeneratedByDb ?? false) || columnIsRowVersion;
                        if (columnKey || columnName.Equals(Settings?.DefaultPrimaryKeyName ?? "Id", StringComparison.InvariantCultureIgnoreCase))
                        {
                            PrimaryKey = column;
                            column.IsReadonly = columnGeneratedByDb ?? true;
                        }

                        if (!column.IsReadonly)
                            mWriteColumns.Add(column);
                    }
                }
            }

            // Init column index 
            mColumns.SetupColumnIndexes();

            // Adding navigation properties
            foreach (var kvp in navProps)
            {
                var mapType = GetPropertyMapping(kvp.Value.PropertyType);
                var foreignKey = mColumns.GetColumn(kvp.Key);
                var propInst = (IPropertyMapping<TEntity>)Activator.CreateInstance(mapType, TableMappingCache.Instance, foreignKey, string.Empty, kvp.Value);
                mProperties.Add(propInst);
            }

            foreach (var kvp in invProps)
            {
                var mapType = GetPropertyMapping(kvp.Value.PropertyType);
                var propInst = (IPropertyMapping<TEntity>)Activator.CreateInstance(mapType, TableMappingCache.Instance, PrimaryKey, kvp.Key, kvp.Value);
                mProperties.Add(propInst);
            }
        }

        private Type GetPropertyMapping(Type propertyType)
        {
            if (propertyType.IsGenericType && typeof(ICollection<>).IsAssignableFrom(propertyType.GetGenericTypeDefinition()))
                return typeof(PropertyListMapping<,>).MakeGenericType(typeof(TEntity), propertyType.GetGenericArguments()[0]);
            
            return typeof(PropertyMapping<,>).MakeGenericType(typeof(TEntity), propertyType);
        }

        private IColumnMapping<TEntity> GetColumnMapping(PropertyInfo property, string name, string fieldName)
        {
            var type = property.PropertyType;
            var underType = Nullable.GetUnderlyingType(type);
            
            return GetColumnMapping(property, underType ?? type, name, fieldName, canBeNull: underType != null);
        }

        private IColumnMapping<TEntity> GetColumnMapping(PropertyInfo property, Type type, string name, string fieldName, bool canBeNull)
        {
            if (type == typeof(string))
            {
                return new ColumnStringMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(int))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnIntNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnIntMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(DateTime))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnDateTimeNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnDateTimeMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(double))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnDoubleNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnDoubleMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(float))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnFloatNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnFloatMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(decimal))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnDecimalNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnDecimalMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(long))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnLongNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnLongMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(short))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnShortNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnShortMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(bool))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnBoolNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnBoolMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(Guid))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnGuidNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnGuidMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(byte))
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)new ColumnByteNullMapping<TEntity>(this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)new ColumnByteMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type == typeof(byte[]))
            {
                return new ColumnBytesMapping<TEntity>(this, property, name, fieldName);
            }
            else if (type.IsEnum)
            {
                return canBeNull
                    ? (IColumnMapping<TEntity>)Activator.CreateInstance(typeof(ColumnEnumNullMapping<,>).MakeGenericType(typeof(TEntity), type), this, property, name, fieldName)
                    : (IColumnMapping<TEntity>)Activator.CreateInstance(typeof(ColumnEnumMapping<,>).MakeGenericType(typeof(TEntity), type), this, property, name, fieldName);
            }

            return null;
        }

        #endregion
        
    }
}
