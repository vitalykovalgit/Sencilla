namespace Sencilla.Component.Security;

/// <summary>
/// Code-declared permission rule (specification pattern). Implementations are
/// discovered from DI and union (OR) with matrix rows — one more grant source,
/// same enforcement points. The expression composes into queries exactly like a
/// parsed matrix constraint: read/update/delete pre- and post-image filtering,
/// in-memory validation for create.
/// </summary>
public interface IPermissionRule<TEntity>
{
    /// <summary>sec.Role id this grant applies to (a System role).</summary>
    int RoleId { get; }

    /// <summary>Actions granted — flags, e.g. Action.Read | Action.Update.</summary>
    Action Action { get; }

    /// <summary>
    /// The constraint for the current request context; null = unconditional grant.
    /// </summary>
    Expression<Func<TEntity, bool>>? Constraint(ISystemVariable sysVars);
}
