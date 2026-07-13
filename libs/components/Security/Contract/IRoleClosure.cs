namespace Sencilla.Component.Security;

/// <summary>
/// Role-graph queries backed by a cached snapshot of sec.Role (raw read — no
/// entity events, so no recursion into security checks). Evicted by
/// <see cref="SecurityCacheSignal"/> on any Role change; short TTL backstop.
/// Implemented by <see cref="RoleClosure"/>.
/// </summary>
public interface IRoleClosure
{
    /// <summary>
    /// Expands role ids with all inherited ancestors (ParentId chain, transitive) —
    /// a role additionally grants everything its parent grants. Unknown ids pass
    /// through untouched.
    /// <code>
    /// // Owner(6, Parent=7) ⊃ Editor(7, Parent=8) ⊃ Viewer(8):
    /// closure.Expand([6])   // → {6, 7, 8} — an Owner also activates Editor/Viewer rows
    /// </code>
    /// </summary>
    HashSet<int> Expand(HashSet<int> roleIds);

    /// <summary>
    /// Reverse closure: every role whose holders activate a matrix row of
    /// <paramref name="roleId"/> — the role itself plus all roles that inherit its
    /// grants. Used to match role-table rows when composing held-role predicates.
    /// Do not mutate the returned set.
    /// <code>
    /// closure.GrantorsOf(7)  // Editor → {7, 6} — Editors and Owners both qualify
    /// closure.GrantorsOf(8)  // Viewer → {8, 7, 6}
    /// </code>
    /// </summary>
    HashSet<int> GrantorsOf(int roleId);

    /// <summary>True when the role exists in the cached sec.Role snapshot.</summary>
    bool Exists(int roleId);

    /// <summary>True when setting <paramref name="parentId"/> as the parent of
    /// <paramref name="roleId"/> would close a cycle.</summary>
    bool WouldCreateCycle(int roleId, int? parentId);
}
