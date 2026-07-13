namespace Sencilla.Component.Security;

/// <summary>
/// Key-agnostic view of a per-entity role row — "user <see cref="UserId"/> holds role
/// <see cref="RoleId"/>". Lets generic validation (e.g. rejecting global-only roles in
/// role tables) work without knowing the secured entity's key type; implement the
/// keyed <see cref="IEntityRoleable{TEntityKey}"/> on real role tables, never this
/// interface alone.
/// </summary>
public interface IEntityRoleable
{
    /// <summary>User holding the role.</summary>
    Guid UserId { get; set; }

    /// <summary>sec.Role id held on the entity (e.g. Owner = 6, Editor = 7, Viewer = 8).</summary>
    int RoleId { get; set; }
}

/// <summary>
/// Convention shape of a per-entity role table (e.g. ProjectUser): each row states
/// "user <see cref="IEntityRoleable.UserId"/> holds role <see cref="IEntityRoleable.RoleId"/>
/// on entity <see cref="EntityId"/>". The permission engine activates a matrix row when
/// the caller holds its role either globally (sec.UserRole) or on the specific row —
/// via this table, discovered through <see cref="SecureWithAttribute"/> (terminal mode).
///
/// Usage — the role table of a secured aggregate root:
/// <code>
/// [SecureWith(typeof(ProjectUser))]
/// public class Project : IEntity&lt;Guid&gt; { }
///
/// public class ProjectUser : IEntity&lt;Guid&gt;, IEntityRoleable&lt;Guid&gt;
/// {
///     public Guid Id { get; set; }
///     [Column("ProjectId")]                       // map EntityId onto the natural FK column
///     public Guid EntityId { get; set; }          // the Project this row grants a role on
///     public Guid UserId { get; set; }            // who holds the role
///     public int RoleId { get; set; }             // sec.Role id (Owner, Editor, Viewer, ...)
/// }
/// </code>
/// Claims-derived roles (Root, Anonymous, User) are global-only and are rejected on
/// writes to roleable tables — holding them per-row is meaningless.
/// </summary>
/// <typeparam name="TEntityKey">Key type of the secured entity.</typeparam>
public interface IEntityRoleable<TEntityKey> : IEntityRoleable
{
    /// <summary>Key of the entity the role is held on.</summary>
    TEntityKey EntityId { get; set; }
}
