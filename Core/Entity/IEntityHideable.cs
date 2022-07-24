
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBaseEntityHideable : IBaseEntity
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
    public interface IEntityHideable<TKey> : IEntity<TKey>, IBaseEntityHideable
    {      
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEntityHideable : IEntityHideable<int>
    {
    }
}
