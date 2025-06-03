
using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column;
using System;
using System.Data;

namespace Sencilla.Infrastructure.SqlMapper.Mapping
{
    /// <summary>
    /// 
    /// </summary>
    public interface IColumnMapping
    {
        /// <summary>
        /// Entity table name
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Entity table schema
        /// </summary>
        string TableSchema { get; }

        /// <summary>
        /// Column name in database. e.g Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Entity field name 
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// return column with square brackets e.g. [Name]
        /// </summary>
        string SafeName { get; }

        /// <summary>
        /// Used to build query and actually return @DbName
        /// </summary>
        string Param { get; }

        /// <summary>
        /// Column alias 
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// return [Table.Alias].[Name]
        /// </summary>
        string AliasReference { get; }

        /// <summary>
        /// return [Table.Reference].[Name]
        /// </summary>
        string Reference { get; }

        /// <summary>
        /// Return sql declaration of column like [Table].[Name] as [Alias]
        /// </summary>
        string Declaration { get; }

        /// <summary>
        /// Column can only be read from database, it will be no possible to insert or update this column
        /// </summary>
        bool IsReadonly { get; set; }

        /// <summary>
        /// Column can only be read from database, it will be no possible to insert or update this column
        /// </summary>
        bool IsRowVersion { get; set; }

        /// <summary>
        /// Column index (or position) in table (IDataReader) 
        /// </summary>
        int? Index { get; set; }

        /// <summary>
        /// The same as Reference property
        /// </summary>
        /// <returns>Return Reference property</returns>
        string ToString();

        /// <summary>
        /// Return true if column is null 
        /// </summary>
        /// <param name="reader"> data reader </param>
        /// <param name="position"> column position </param>
        /// <returns></returns>
        bool IsNull(IDataReader reader, int position);

        /// <summary>
        /// Return true if this column exist in reader.
        /// Checked by name 
        /// </summary>
        bool IsColumnExist(IDataReader reader);

        /// <summary>
        /// Return null if column does not exist otherwise index of column 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        int? GetColumnIndexByAlias(IDataReader reader);

        /// <summary>
        /// Return null if column does not exist otherwise index of column 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        int? GetColumnIndexByName(IDataReader reader);

        /// <summary>
        /// Return column value from reader 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="indexShift"></param>
        /// <returns></returns>
        object GetValue(IDataReader reader, int indexShift);
    }

    /// <inheritdoc />
    /// <summary>
    /// Map specific column in DB to the entity field 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IColumnMapping<TEntity> : IColumnMapping
    {
        Type FieldType { get; }

        /// <summary>
        /// Get field as a string 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        object GetField(TEntity entity);

        /// <summary>
        /// Set field using index shift, used to read navigation properties
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="reader"></param>
        /// <param name="indexShift"> Used to read navigation properties </param>
        /// <returns></returns>
        TEntity SetField(TEntity entity, IDataReader reader, ColumnPos pos/*, int indexShift*/);

        /// <summary>
        /// Clone column mapping 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        IColumnMapping<TEntity> Clone(ITableMapping table);
    }
}
