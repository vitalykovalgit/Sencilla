using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Mapping
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITableMapping
    {
        /// <summary>
        /// Database table name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        string SafeName { get; }

        /// <summary>
        /// Database schema of the table 
        /// </summary>
        string Schema { get; }

        /// <summary>
        /// Table alias e.g. (Table as Alias)
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// Retun table reference, e.g. [DbSchema].[DbName]
        /// </summary>
        string Reference { get; }

        /// <summary>
        /// Return sql declaration of this table (e.g. [DbSchema].[DbName] as [Alias])
        /// </summary>
        string Declaration { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string ToString();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        bool HasPrimaryKey(IDataReader reader);

        /// <summary>
        /// Primary key column mapping. It return null if there is no primary key 
        /// </summary>
        IColumnMapping PrimaryKey { get; }

        /// <summary>
        /// 
        /// </summary>
        IColumnCollection Columns { get; }

        /// <summary>
        /// List of column that can be written. 
        /// It is Columns property without readonly columns
        /// </summary>
        IColumnCollection WriteColumns { get; }

        /// <summary>
        /// List of properties 
        /// </summary>
        IPropertyCollection Properties { get; }

        /// <summary>
        /// return property by name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IPropertyMapping GetProperty(string name);
    }

    /// <inheritdoc />
    /// <summary>
    /// Map entity to database's table 
    /// </summary>
    public interface ITableMapping<TEntity> : ITableMapping 
    {
        /// <summary>
        /// Primary key column mapping. It return null if there is no primary key 
        /// </summary>
        new IColumnMapping<TEntity> PrimaryKey { get; }

        /// <summary>
        /// List of columns 
        /// </summary>
        new IColumnCollection<TEntity> Columns { get; }
        
        /// <summary>
        /// List of column that can be written. 
        /// It is Columns property without readonly columns
        /// </summary>
        new IColumnCollection<TEntity> WriteColumns { get; }

        /// <summary>
        /// Navigation properties 
        /// </summary>
        new IPropertyCollection<TEntity> Properties { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="previous"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        TEntity ToEntity(DbDataReader reader, TEntity previous, ReadSession session, int recursionDepth = 0);

        /// <summary>
        /// Create instance of TEntity and initialize it from IDataReader
        /// </summary>
        /// <param name="reader">  </param>
        /// <returns> Instance if TEntity initialized from IDataReader </returns>
        TEntity ReadFirstOrDefault(DbDataReader reader);
        Task<TEntity> ReadFirstOrDefaultAsync(DbDataReader reader, CancellationToken? token = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        List<TEntity> ReadList(DbDataReader reader);
        Task<List<TEntity>> ReadListAsync(DbDataReader reader, CancellationToken? token = null);
    }
}
