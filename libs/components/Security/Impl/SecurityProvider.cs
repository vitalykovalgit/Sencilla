namespace Sencilla.Component.Security;

/// <summary>
/// 
/// </summary>
public class SecurityProvider(IServiceProvider provider, IMemoryCache cache) : ISecurityProvider
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(180);
    private const string AllPermissionsCacheKey = "security_all_permissions";

    public async Task<IQueryable<Matrix>> GetAllPermissions(CancellationToken token) 
    {
        var allPermissions = await cache.GetOrCreateAsync(AllPermissionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
            var securityProviders = provider.GetRequiredService<IEnumerable<ISecurityDeclaration>>();
            var tasks = securityProviders.Select(p => p.Permissions(token));
            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r).AsQueryable();
        });

        return allPermissions!;
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



