
namespace Sencilla.Core;

/// <summary>
/// Used to check if entity is deleatble
/// without generic parameters
/// </summary>
public interface IEntityDeleteable : IBaseEntity
{
}


/// <summary>
/// Fired when entity is going to be updated 
/// </summary>
public class EntityDeletingEvent<TEntity> : EntityBaseEvent<TEntity>
{
}

/// <summary>
/// Fired when entity is updated 
/// </summary>
public class EntityDeletedEvent<TEntity> : EntityBaseEvent<TEntity>
{
}