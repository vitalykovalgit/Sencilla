
namespace Sencilla.Core;

/// <summary>
/// Marks an entity that is an immutable, point-in-time snapshot of another entity.
/// A snapshot shares the primary key of the entity it freezes (1:1, shared PK) and stores a
/// versioned payload captured once at creation and never updated, so historical records stay
/// accurate even after their source rows are edited or deleted.
/// </summary>
public interface IEntitySnapshot : IBaseEntity
{
    /// <summary>
    /// Shape version of <see cref="Data"/>. Bump when the payload contract changes so older snapshots stay readable.
    /// </summary>
    int SchemaVersion { get; set; }

    /// <summary>
    /// The frozen JSON payload, captured once at creation and never updated.
    /// </summary>
    string? Data { get; set; }
}
