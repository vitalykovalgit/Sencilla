using System.ComponentModel.DataAnnotations.Schema;

namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

/// <summary>
/// A full-featured test entity that implements all repository lifecycle interfaces.
/// Used across all repository test suites.
/// </summary>
// The MERGE builders name the table after [Table] or the class; without it EF would name it after the DbSet.
[Table(nameof(TestProduct))]
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

    // JSON dictionary target for JsonMergeAsync enforcement tests. Not mapped —
    // the permission check runs before the SQL, so mapping is never resolved.
    [NotMapped]
    public Dictionary<string, string>? Meta { get; set; }
}
