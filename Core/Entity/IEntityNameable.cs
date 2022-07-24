
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBaseEntityNameable : IBaseEntity
    {
        /// <summary>
        /// Name of the entity 
        /// </summary>
        string Name { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEntityNameable<TKey> : IEntity<TKey>, IBaseEntityNameable
    {
        
    }
}
