using System.Collections.Generic;

namespace Sencilla.Infrastructure.SqlMapper.Mapping
{
    /// <inheritdoc />
    /// <summary>
    /// Map entity to database's table 
    /// </summary>
    public interface IPropertyCollection : IEnumerable<IPropertyMapping>
    {
        /// <summary>
        /// Return columns declarations, the same as Declarations
        /// </summary>
        /// <returns></returns>
        string ToString();
    }


    public interface IPropertyCollection<TEntity> : IEnumerable<IPropertyMapping<TEntity>>
    {
        /// <summary>
        /// Return columns declarations, the same as Declarations
        /// </summary>
        /// <returns></returns>
        string ToString();
    }
}
