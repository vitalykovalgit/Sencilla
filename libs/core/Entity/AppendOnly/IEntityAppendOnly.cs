
namespace Sencilla.Core;

/// <summary>
/// Marks an entity as append-only: rows may be inserted but never updated or deleted.
/// A change is expressed by inserting a new row (a new version), never by mutating an existing
/// one, so every row is immutable and history is preserved. Enforced at the persistence layer
/// (an EF SaveChanges interceptor rejects Modified/Deleted states for these entities).
///
/// Such entities deliberately do NOT implement <see cref="IEntityUpdateable"/> or
/// <see cref="IEntityDeleteable"/>, so the auto-CRUD API exposes create (PUT) but degrades
/// update (POST) and delete (DELETE) to HTTP 501. Physical removal, when truly required, is an
/// out-of-band maintenance action that bypasses the application.
/// </summary>
public interface IEntityAppendOnly : IBaseEntity
{
}
