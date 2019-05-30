using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEntityNameable : IEntity
    {
        /// <summary>
        /// Name of the entity 
        /// </summary>
        string Name { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IEntityNameable<TKey> : IEntity<TKey>, IEntityNameable
    {
        
    }
}
