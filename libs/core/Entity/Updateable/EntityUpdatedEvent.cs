
namespace Sencilla.Core;

/// <summary>
/// Fired when entity is updated 
/// </summary>
public class EntityUpdatedEvent<TEntity> : EntityBaseEvent<TEntity>
{
    /// <summary>
    /// Database query for the written rows (post-image — state after the save, read
    /// inside the ambient transaction). Constraint handlers narrow it with Where();
    /// the publishing repository rolls the write back when the narrowed query loses
    /// rows (a row may not be moved out of the writer's own permission scope).
    /// </summary>
    public IQueryable<TEntity>? DbEntities { get; set; }
}