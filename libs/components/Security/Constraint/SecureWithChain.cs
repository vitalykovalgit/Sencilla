namespace Sencilla.Component.Security;

/// <summary>
/// Resolved [SecureWith] chain of one entity — the fixed, schema-derived path the
/// permission engine walks to answer "does the caller hold role R on this row?".
/// Produced and validated by <see cref="SecureWithChains"/>; consumed by
/// <see cref="SecureWithFilter"/> to compose the EXISTS predicate.
///
/// Examples of resolved shapes:
/// <code>
/// // Project      → terminal:  Hops = [],                       Link = ProjectUser
/// // ProjectItem  → 1 hop:     Hops = [(ProjectItem, ProjectId)], Link = ProjectUser
/// // ProjectFile  → 2 hops:    Hops = [(ProjectFile, ItemId), (ProjectItem, ProjectId)]
/// </code>
/// </summary>
public sealed class SecureWithChain
{
    /// <summary>The secured entity the chain starts from.</summary>
    public required Type Entity { get; init; }

    /// <summary>The entity carrying the terminal declaration (owns the role table).</summary>
    public required Type Terminal { get; init; }

    /// <summary>The role table type (implements <see cref="IEntityRoleable{TEntityKey}"/>).</summary>
    public required Type LinkType { get; init; }

    /// <summary>Key type of <see cref="LinkType"/> itself (its IEntity&lt;TKey&gt; argument).</summary>
    public required Type LinkKeyType { get; init; }

    /// <summary>Key type of <see cref="Terminal"/> — the type of the role table's EntityId.</summary>
    public required Type TerminalKeyType { get; init; }

    /// <summary>sec.Role id provisioned for the creator of the terminal entity.</summary>
    public required int CreatorRole { get; init; }

    /// <summary>
    /// FK hops from <see cref="Entity"/> toward <see cref="Terminal"/>, in walk order.
    /// Each element is (the entity declaring the hop, its Via FK property). Empty when
    /// the entity is itself terminal. The LAST hop's Via yields the terminal key value,
    /// so only intermediate hops need a join — see <see cref="SecureWithFilter"/>.
    /// </summary>
    public required IReadOnlyList<(Type Entity, PropertyInfo Via)> Hops { get; init; }
}
