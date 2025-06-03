using System.Collections.Generic;

namespace Sencilla.Infrastructure.SqlMapper.Mapping
{
    public interface IColumns
    {
        /// <summary>
        /// Return columns declarations, the same as Declarations
        /// </summary>
        /// <returns></returns>
        string ToString();

        /// <summary>
        /// Returns list of columns safe names separated by comma
        /// </summary>
        string Names { get; }

        /// <summary>
        /// Returns list of params separated by comma 
        /// </summary>
        string Params { get; }

        /// <summary>
        /// List of column alias references separated by comma
        /// </summary>
        string AliasReferences { get; }

        /// <summary>
        /// List of column declarations separated by comma
        /// </summary>
        string Declarations { get; }

        /// <summary>
        /// Returns Declaration = Param, used by UPDATE statements
        /// </summary>
        string ReferenceParams { get; }

        /// <summary>
        /// Columns count in collection 
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Get column from collection by field name 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        IColumnMapping GetColumn(string fieldName);
    }

    public interface IColumnCollection : IEnumerable<IColumnMapping>, IColumns
    {
        
    }

    /// <inheritdoc />
    /// <summary>
    /// Map entity to database's table 
    /// </summary>
    public interface IColumnCollection<TEntity> : IEnumerable<IColumnMapping<TEntity>>, IColumns
    {
    }
}
