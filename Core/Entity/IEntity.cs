using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// Sometimes need to check if a class is an entity, 
    /// using generic class is very inconvinient for this 
    /// 
    /// </summary>
    public interface IEntity
    {
    }

    /// <summary>
    /// Base interface for all the entity 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface IEntity<TKey> : IEntity
    {
        /// <summary>
        /// Every entity must have unique identifier 
        /// </summary>
        TKey Id { get; set; }
    }
}
