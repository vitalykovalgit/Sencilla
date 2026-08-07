
namespace Sencilla.Component.Security;

/// <summary>
/// Provide permissions
/// </summary>
public interface ISecurityProvider
{
    Task<IEnumerable<Matrix>> Permissions<TEntity>(CancellationToken token, Action? action = null);

    /// <summary>
    /// Rows declared for a resource NAME rather than an entity type — the operation resources
    /// (<c>admin.orders.payment</c>) that have no <c>TEntity</c> to derive a name from.
    ///
    /// Returns the DECLARATION, not an authorization decision: rows for every role, unfiltered by who
    /// is asking. Callers must intersect with the caller's role set themselves, or go through
    /// <see cref="IPermissionChecker"/>, which does.
    /// </summary>
    Task<IEnumerable<Matrix>> Permissions(CancellationToken token, string resource, Action? action = null);

    /// <summary>
    /// Every permission row from every source (DB, attributes, fluent API), cached.
    /// Used by startup validation to type-check constraint strings; per-request
    /// enforcement should use <see cref="Permissions{TEntity}"/>.
    /// </summary>
    Task<IQueryable<Matrix>> GetAllPermissions(CancellationToken token);
}



