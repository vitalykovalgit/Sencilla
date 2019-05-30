using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEntityHideable : IEntity
    {
        /// <summary>
        /// True if entity is hidden 
        /// </summary>
        bool Hidden { get; set; }
    }

    /// <summary>
    /// This interface is used to 
    /// allow hide show functionality on entity 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IEntityHideable<TKey> : IEntity<TKey>, IEntityHideable
    {
       
    }
}
