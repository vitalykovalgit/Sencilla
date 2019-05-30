using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Core.Entity
{
    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IEntityDeleteable : IEntity
    {
    }

    /// <summary>
    /// This interface is used to allow deleting of the entity 
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
	public interface IEntityDeleteable<TKey> : IEntity<TKey>, IEntityDeleteable
    {
    }
}
