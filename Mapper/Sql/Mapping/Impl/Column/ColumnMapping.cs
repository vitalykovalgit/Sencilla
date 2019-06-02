using System;
using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    /// <inheritdoc />
    /// <summary>
    /// Map column to entity's property
    /// </summary>
    public abstract class ColumnMapping<TEntity, TFieldType> : IColumnMapping<TEntity>
    {
        #region Members 

        public Type FieldType { get; }
        
        /// <summary>
        /// Table to which this column belongs to 
        /// </summary>
        protected ITableMapping Table { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected Func<TEntity, TFieldType> Getter;

        /// <summary>
        /// 
        /// </summary>
        protected Action<TEntity, TFieldType> Setter;
        
        #endregion 

        /// <summary>
        /// Initialize column mapping 
        /// </summary>
        protected ColumnMapping(ITableMapping table, PropertyInfo info, string name, string fieldName)
        {
            Name = name;
            FieldName = fieldName;

            Table = table;
            IsReadonly = false;

            FieldType = info.PropertyType;

            // TODO: Choose between memory or spead 
            // As table name and alias are static, then init fields in constructor
            /*
            SafeName = $"[{Name}]";
            Param = $"@{Name}";
            Alias = $"{Table.Alias}_{Name}";
            Reference = $"{Table}.[{Name}]";
            Declaration = $"{Reference} AS [{Alias}]";
            */

            Getter = (Func<TEntity, TFieldType>)Delegate.CreateDelegate(typeof(Func<TEntity, TFieldType>), info.GetGetMethod());
            Setter = (Action<TEntity, TFieldType>)Delegate.CreateDelegate(typeof(Action<TEntity, TFieldType>), info.GetSetMethod());
        }
        /*
        protected ColumnMapping(ITableMapping table, string name, string fieldName, bool isReadoonly, Func<TEntity, TFieldType> getter, Action<TEntity, TFieldType> setter)
        {
            Name = name;
            FieldName = fieldName;

            Table = table;
            IsReadonly = isReadoonly;

            Getter = getter;
            Setter = setter;
        }
        */
        protected ColumnMapping()
        {
        }

        /// <summary>
        /// Column's table name
        /// </summary>
        public string TableName => Table.Name;

        /// <summary>
        /// Column's table schema
        /// </summary>
        public string TableSchema => Table.Schema;

        /// <inheritdoc />
        /// <summary>
        /// Column name in database which represents entity field 
        /// </summary>
        public string Name { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// Name of the field 
        /// </summary>
        public string FieldName { get; protected set; }

        /// <inheritdoc />
        /// <summary>
        /// return columns with square brackets e.g. [Name]
        /// </summary>
        public string SafeName => $"[{Name}]";

        /// <inheritdoc />
        /// <summary>
        /// Parameter in sql command, returns '@Name'
        /// </summary>
        public string Param => $"@{Name}";

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string Alias => $"{Table.Alias}_{Name}";

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string AliasReference => $"[{Table.Alias}].[{Name}]";

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string Reference => $"{Table.Reference}.[{Name}]";

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public string Declaration => $"{AliasReference} AS [{Alias}]";

        /// <inheritdoc />
        /// <summary>
        /// Column can only be read from database, it will be no possible to insert or update this column
        /// </summary>
        public bool IsReadonly { get; set; }

        /// <summary>
        /// Column can only be read from database, it will be no possible to insert or update this column
        /// </summary>
        public bool IsRowVersion { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Column position in IDataReader 
        /// This index is used to speed up read process 
        /// </summary>
        public int? Index { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// The same as Reference
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return AliasReference;
        }
        /*
        public int FindColumnIndexByAlias(IDataReader reader)
        {
            //Index = reader.GetOrdinal(Alias);
            return reader.GetOrdinal(Alias);
        }

        public int FindColumnIndexByName(IDataReader reader)
        {
            return reader.GetOrdinal(Name);
        }
        */
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public TEntity SetField(TEntity entity, IDataReader reader)
        {
            if (Index == null)
                Index = GetColumnIndexByAlias(reader) ?? GetColumnIndexByName(reader);

            if (entity != null)
                Setter(entity, ReadValue(reader, Index.Value));

            return entity;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public TEntity SetField(TEntity entity, IDataReader reader, ColumnPos pos)
        {
            if (entity != null)
            {
                if (pos.Poition == null)
                    pos.Poition = GetColumnIndexByAlias(reader) ?? GetColumnIndexByName(reader);

                Setter(entity, ReadValue(reader, pos.Poition.Value));
            }

            return entity;
        }

        public object GetField(TEntity entity)
        {
            return Getter(entity);
        }

        public object GetValue(IDataReader reader, int indexShift)
        {
            return ReadValue(reader, indexShift + Index.Value);
        }

        public bool IsNull(IDataReader reader, int position)
        {
            return reader.IsDBNull(position);
        }

        public bool IsColumnExist(IDataReader reader)
        {
            return reader.HasColumn(Alias);
        }

        /// <summary>
        /// Retrive column index by alisa 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public int? GetColumnIndexByAlias(IDataReader reader)
        {
            return reader.GetColumnIndex(Alias);
        }

        public int? GetColumnIndexByName(IDataReader reader)
        {
            return reader.GetColumnIndex(Name);
        }

        protected ColumnMapping<TEntity, TFieldType> Clone(ColumnMapping<TEntity, TFieldType> origin, ITableMapping table)
        {
            Name = origin.Name;
            FieldName = origin.FieldName;

            Table = table;
            IsReadonly = origin.IsReadonly;

            Getter = origin.Getter;
            Setter = origin.Setter;
            return this;
        }

        public abstract IColumnMapping<TEntity> Clone(ITableMapping table);
        
        /// <summary>
        /// Reader column by specific index
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected abstract TFieldType ReadValue(IDataReader reader, int index);
    }
}
