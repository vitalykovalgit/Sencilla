
namespace Sencilla.Component.Security;

/// <summary>
/// Provide permissions
/// </summary>
public interface ISecurityProvider
{
    Task<IEnumerable<Matrix>> Permissions<TEntity>(CancellationToken token, Action? action = null);

    /// <summary>
    /// Every permission row from every source (DB, attributes, fluent API), cached.
    /// Used by startup validation to type-check constraint strings; per-request
    /// enforcement should use <see cref="Permissions{TEntity}"/>.
    /// </summary>
    Task<IQueryable<Matrix>> GetAllPermissions(CancellationToken token);
}



