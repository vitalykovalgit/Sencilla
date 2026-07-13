
namespace Sencilla.Core;

public class EntityCreatedEvent<TEntity>: EntityBaseEvent<TEntity>
{
    /// <summary>
    /// Database query for the inserted rows (post-image — state after the save, read
    /// inside the ambient transaction). Constraint handlers narrow it with Where();
    /// the publishing repository rolls the insert back when the narrowed query loses
    /// rows (a creator must satisfy the constraints of every row it creates).
    /// Narrowing is lazy — the query executes only after ALL handlers ran, so role
    /// provisioning (the creator's role row) is already visible to it regardless of
    /// handler order. Null when the publisher cannot provide a DB view (non-EF paths);
    /// handlers then fall back to validating <see cref="EntityBaseEvent{TEntity}.Entities"/>.
    /// </summary>
    public IQueryable<TEntity>? DbEntities { get; set; }
}
