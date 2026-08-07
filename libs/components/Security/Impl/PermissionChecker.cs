using Microsoft.Extensions.Logging;

namespace Sencilla.Component.Security;

/// <summary>Contract and rationale in <see cref="IPermissionChecker"/>.</summary>
public class PermissionChecker(
    ISystemVariable sysVars,
    ISecurityProvider provider,
    IUserRoleResolver roles,
    IServiceProvider services) : IPermissionChecker
{
    public async Task DemandAsync(string resource, Action action, CancellationToken token)
    {
        if (!await HasAsync(resource, action, token))
            throw new ForbiddenException($"No permission for '{resource}'.");
    }

    public async Task<bool> HasAsync(string resource, Action action, CancellationToken token)
    {
        var user = sysVars.GetCurrentUser();
        var direct = roles.Resolve(user);

        // Root is break-glass, checked before any matrix lookup — immune to a bad seed, a stale cache
        // and admin lockout, exactly as the entity path treats it.
        if (direct.Contains((int)RoleType.Root))
            return true;

        var roleIds = roles.ResolveExpanded(user);

        // Root access for the lookup itself: reading the matrix must not recurse into a matrix check.
        using var rootAccess = Access.Root();

        var rows = await provider.Permissions(token, resource, action);

        return rows.Any(r => roleIds.Contains(r.RoleId) && Unconditional(r));
    }

    public async Task<Dictionary<string, int>> GrantedAsync(CancellationToken token)
    {
        var user = sysVars.GetCurrentUser();

        if (roles.Resolve(user).Contains((int)RoleType.Root))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [IPermissionChecker.AllResources] = (int)Action.All
            };

        var roleIds = roles.ResolveExpanded(user);

        using var rootAccess = Access.Root();

        var all = await provider.GetAllPermissions(token);
        var granted = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in all.Where(r => r.Resource != null))
        {
            if (!roleIds.Contains(row.RoleId) || !Unconditional(row))
                continue;

            granted[row.Resource!] = granted.TryGetValue(row.Resource!, out var mask)
                ? mask | row.Action
                : row.Action;
        }

        return granted;
    }

    /// <summary>
    /// A constrained row cannot grant here, and that is not a limitation to work around.
    ///
    /// A <c>Constraint</c> is a predicate over an ENTITY (<c>userId={user}.Id</c>) evaluated by
    /// composing it into a query. This API answers "may the caller perform this operation" with no
    /// entity in hand, so the predicate is unevaluable — and an unevaluable condition treated as
    /// satisfied is a grant nobody wrote. Logged, because a constrained row on an operation resource
    /// is a seed mistake rather than a runtime state.
    /// </summary>
    private bool Unconditional(Matrix row)
    {
        if (string.IsNullOrWhiteSpace(row.Constraint))
            return true;

        services.GetService<ILogger<PermissionChecker>>()?.LogWarning(
            "Matrix row {Role}/{Resource} carries a constraint '{Constraint}'. Constraints are entity predicates and cannot be evaluated for an operation permission — the row grants nothing here.",
            row.RoleId, row.Resource, row.Constraint);

        return false;
    }
}
