namespace Sencilla.Component.Security;

/// <summary>
/// The caller's role set — the one place that answers "which matrix rows activate for this user".
///
/// Extracted from <see cref="SecurityConstraintHandler{TEntity}"/> so that entity enforcement and
/// endpoint enforcement cannot drift: two implementations of "which roles does this principal hold"
/// is two answers to an authorization question, and the second one is always the one nobody tests.
/// </summary>
public interface IUserRoleResolver
{
    /// <summary>
    /// Roles held DIRECTLY: Anonymous for everyone, User for any authenticated identity, plus the
    /// persisted <c>sec.UserRole</c> assignments. Deliberately NOT expanded through the parent chain —
    /// callers that need the break-glass <see cref="RoleType.Root"/> check must see it before
    /// inheritance widens the set.
    /// </summary>
    HashSet<int> Resolve(User? user);

    /// <summary>
    /// <see cref="Resolve"/> plus every inherited ancestor (<c>sec.Role.Parent</c>, transitive) — the
    /// set a matrix row's <c>Role</c> is matched against.
    /// </summary>
    HashSet<int> ResolveExpanded(User? user);

    /// <summary>The expanded set for the ambient caller. Convenience over <see cref="ResolveExpanded"/>.</summary>
    HashSet<int> Current();
}
