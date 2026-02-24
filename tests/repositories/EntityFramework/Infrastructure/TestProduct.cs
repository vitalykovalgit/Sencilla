namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

/// <summary>
/// A full-featured test entity that implements all repository lifecycle interfaces.
/// Used across all repository test suites.
/// </summary>
public class TestProduct :
    IEntity<int>,
    IEntityCreateableTrack,   // IEntityCreateable + CreatedDate
    IEntityUpdateableTrack,   // IEntityUpdateable + UpdatedDate
    IEntityDeleteable,        // supports hard delete
    IEntityRemoveable         // supports soft delete (inherits IEntityUpdateable)
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    // IEntityCreateableTrack
    public DateTime CreatedDate { get; set; }

    // IEntityUpdateableTrack
    public DateTime UpdatedDate { get; set; }

    // IEntityRemoveable
    public DateTime? DeletedDate { get; set; }
}
