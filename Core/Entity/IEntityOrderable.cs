
namespace Sencilla.Core
{
    /// <summary>
    /// This interface is used to allow sorting of entity 
    /// </summary>
    public interface IBaseEntityOrderable : IBaseEntity
    {   
        /// <summary>
        /// Order of the entity 
        /// </summary>
        int Order { get; set; }
    }

    /// <summary>
    /// This interface is used to allow sorting of entity 
    /// </summary>
    public interface IEntityOrderable<TKey> : IEntity<TKey>, IBaseEntityOrderable
    {
    }

    public interface IEntityOrderable : IEntityOrderable<int>
    {
    }
}
