using Microsoft.Extensions.Logging;

namespace Sencilla.Component.Security;

/// <summary>
///
/// </summary>
public class SecurityProvider(IServiceProvider provider, IMemoryCache cache, SecurityCacheSignal signal) : ISecurityProvider
{
    // Short TTL is the backstop only — the signal evicts on Matrix/Role/UserRole
    // writes; the TTL covers other nodes and the pre-commit eviction race.
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private const string AllPermissionsCacheKey = "security_all_permissions";

    public async Task<IQueryable<Matrix>> GetAllPermissions(CancellationToken token)
    {
        var allPermissions = await cache.GetOrCreateAsync(AllPermissionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
            entry.AddExpirationToken(signal.Token);
            var securityProviders = provider.GetRequiredService<IEnumerable<ISecurityDeclaration>>();
            var tasks = securityProviders.Select(p => p.Permissions(token));
            var results = await Task.WhenAll(tasks);
            var rows = results.SelectMany(r => r).ToList();
            ResolveRoleNames(rows);
            return rows.AsQueryable();
        });

        return allPermissions!;
    }

    /// <summary>
    /// Attribute/builder-declared rows carry only a role NAME; resolve it to the
    /// sec.Role id lazily (cached together with the rows, same eviction), so code
    /// sources can reference dynamic roles. Unresolvable rows are dropped with a
    /// warning — an inert grant is invisible, a logged one is diagnosable.
    /// </summary>
    private void ResolveRoleNames(List<Matrix> rows)
    {
        var unresolved = rows.Where(r => r.RoleId == 0 && !string.IsNullOrWhiteSpace(r.Role)).ToList();
        if (unresolved.Count == 0)
            return;

        var logger = provider.GetService<ILogger<SecurityProvider>>();
        // Raw read — framework internal, no entity events.
        var byName = provider.GetService<IReadRepository<Role, int>>()?.Query.ToList()
            .Where(r => r.Name != null)
            .GroupBy(r => r.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var row in unresolved)
        {
            if (byName != null && byName.TryGetValue(row.Role, out var id))
            {
                row.RoleId = id;
            }
            else
            {
                logger?.LogWarning("Permission row for resource '{Resource}' references unknown role '{Role}' — the grant is inactive.", row.Resource, row.Role);
                rows.Remove(row);
            }
        }
    }
    public Task<IEnumerable<Matrix>> Permissions<TEntity>(CancellationToken token, Action? action = null)
    {
        var resource = ResourceName<TEntity>();
        return Permissions(token, resource, action);
    }

    public async Task<IEnumerable<Matrix>> Permissions(CancellationToken token, string resource, Action? action = null)
    {
        // get permission for resource
        var q = (await GetAllPermissions(token)).Where(p => p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase));

        // action 
        if (action != null)
            q = q.Where(p => (p.Action & (int)action.Value) == (int)action.Value);

        // roles 

        return q;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string ResourceName<TEntity>()
    {
        return ResourceName(typeof(TEntity));
    }

    public static string ResourceName(Type entityType)
    {
        // try to get attribute 
        var resourceAttr = entityType.GetCustomAttribute<ResourceAttribute>();
        return resourceAttr?.Name ?? entityType.Name;
    }
}



