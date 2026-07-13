
namespace Sencilla.Core;

/// <summary>
/// Fired when entity is going to be updated 
/// </summary>
public class EntityUpdatingEvent<TEntity> : EntityBaseEvent<TEntity>
{
    /// <summary>
    /// Database query for the target rows (pre-image — current DB state, no events
    /// applied). Constraint handlers narrow it with Where(); the publishing repository
    /// denies the whole batch when the narrowed query loses existing rows.
    /// Enforcement must never trust <see cref="EntityBaseEvent{TEntity}.Entities"/>
    /// here — those are the client-supplied objects.
    /// </summary>
    public IQueryable<TEntity>? DbEntities { get; set; }
}
