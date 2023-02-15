
namespace Sencilla.Core;

/// <summary>
/// Allow entity to be updatable 
/// </summary>
public interface IEntityUpdateable : IBaseEntity
{
}

/// <summary>
/// 
/// </summary>
public interface IEntityUpdateableTrack : IEntityUpdateable
{
    /// <summary>
    /// Last updated date time 
    /// </summary>
    DateTime UpdatedDate { get; set; }
}


/// <summary>
/// Fired when entity is going to be updated 
/// </summary>
public class EntityUpdatingEvent<TEntity> : EntityBaseEvent<TEntity>
{
}

/// <summary>
/// Fired when entity is updated 
/// </summary>
public class EntityUpdatedEvent<TEntity> : EntityBaseEvent<TEntity>
{
}
