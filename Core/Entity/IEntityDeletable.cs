
namespace Sencilla.Core
{
    /// <summary>
    /// Used to check if entity is deleatble
    /// without generic parameters
    /// </summary>
    public interface IBaseEntityDeleteable : IBaseEntity
    {
    }

    /// <summary>
    /// This interface is used to allow deleting of the entity 
    /// </summary>
	public interface IEntityDeleteable<TKey> : IEntity<TKey>, IBaseEntityDeleteable
    {
    }

    /// <summary>
    /// Impelement IEntityDeleteable interface with default type as int
    /// </summary>
    public interface IEntityDeleteable : IEntityDeleteable<int>
    {
    }
}
