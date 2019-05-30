using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// This interface is used to allow sorting of entity 
    /// </summary>
    public interface IEntityOrderable : IEntity
    {   
        /// <summary>
        /// Order of the entity 
        /// </summary>
        int Order { get; set; }
    }

    /// <summary>
    /// This interface is used to allow sorting of entity 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IEntityOrderable<TKey> : IEntity<TKey>, IEntityOrderable
    {
    }
}
