
namespace Sencilla.Core
{
    /// <summary>
    /// Sometimes need to check if a class is an entity, 
    /// using generic class is very inconvinient for this 
    /// </summary>
    public interface IBaseEntity
    {
    }

    /// <summary>
    /// Base interface for all the entity 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface IEntity<TKey> : IBaseEntity
    {
        /// <summary>
        /// Every entity must have unique identifier 
        /// </summary>
        TKey Id { get; set; }
    }

    /// <summary>
    /// Use int as default parameter for generic argument 
    /// </summary>
    public interface IEntity : IEntity<int>
    { 
    }
}
