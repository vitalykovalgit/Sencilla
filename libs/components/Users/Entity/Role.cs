
namespace Sencilla.Component.Users;

[Table(nameof(Role), Schema = "sec")]
public class Role: IEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// Inheritance: this role additionally grants everything its parent grants
    /// (expanded transitively at role resolution — Owner ⊃ Editor ⊃ Viewer is
    /// Owner.Parent = Editor, Editor.Parent = Viewer). Cycles are rejected at
    /// save time. A role's SCOPE is not a property of the role — it is where the
    /// role is held: globally (sec.UserRole) or on a row (a [SecureWith] role table).
    /// </summary>
    [Column("Parent")]
    public int? ParentId { get; set; }
}
