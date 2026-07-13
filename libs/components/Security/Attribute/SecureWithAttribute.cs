namespace Sencilla.Component.Security;

/// <summary>
/// Declares where an entity's per-row roles come from — the single source the
/// permission engine walks to activate matrix rows by on-the-row holding.
/// One attribute, two modes, disambiguated by what <see cref="Source"/> is:
///
/// <b>Terminal mode</b> — Source implements <see cref="IEntityRoleable{TEntityKey}"/>:
/// roles for this entity live directly in that table. When a user creates the
/// entity, a role row (UserId = creator, EntityId = new id, RoleId = <see cref="CreatorRole"/>)
/// is provisioned in the same ambient transaction — the entity can never exist
/// without its creator's role under role-held constraints.
/// <code>
/// [SecureWith(typeof(ProjectUser))]                    // creator becomes Owner (default)
/// public class Project : IEntity&lt;Guid&gt; { }
///
/// [SecureWith(typeof(TeamUser), CreatorRole = 7)]      // creator becomes Editor instead
/// public class Team : IEntity&lt;int&gt; { }
/// </code>
///
/// <b>Hop mode</b> — Source is another secured entity: this entity has no role
/// table of its own; its per-row roles resolve through the <see cref="Via"/> FK to
/// the parent. Chains compose transitively and must terminate in a terminal
/// declaration (validated at startup, cycles rejected).
/// <code>
/// [SecureWith(typeof(Project), Via = nameof(ProjectItem.ProjectId))]
/// public class ProjectItem : IEntity&lt;Guid&gt; { public Guid ProjectId { get; set; } }
///
/// [SecureWith(typeof(ProjectItem), Via = nameof(ProjectFile.ItemId))]  // grandchild
/// public class ProjectFile : IEntity&lt;Guid&gt; { public Guid ItemId { get; set; } }
///
/// // A role table may secure ITSELF through the entity it protects, so sharing
/// // management ("only Owners may add members") uses the same machinery:
/// [SecureWith(typeof(Project), Via = nameof(ProjectUser.EntityId))]
/// public class ProjectUser : IEntity&lt;Guid&gt;, IEntityRoleable&lt;Guid&gt; { }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SecureWithAttribute(Type source) : Attribute
{
    /// <summary>
    /// Role table (a type implementing <see cref="IEntityRoleable{TEntityKey}"/> → terminal mode)
    /// or the parent entity whose roles this entity inherits (→ hop mode).
    /// </summary>
    public Type Source { get; } = source;

    /// <summary>
    /// Hop mode only: name of the FK property on THIS entity pointing at
    /// <see cref="Source"/>'s key (e.g. <c>nameof(ProjectItem.ProjectId)</c>).
    /// Required for hops, forbidden for terminal declarations — validated at startup.
    /// </summary>
    public string? Via { get; set; }

    /// <summary>
    /// Terminal mode only: sec.Role id written to the creator's role row.
    /// Defaults to the seeded 'Owner' role (id 6).
    /// </summary>
    public int CreatorRole { get; set; } = 6;
}
