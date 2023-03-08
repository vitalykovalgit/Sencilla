
namespace Sencilla.Component.Security;

/// <summary>
/// 
/// </summary>
public class SecurityProvider: ISecurityProvider
{
    IQueryable<Matrix>? allPermissions;
    IEnumerable<ISecurityDeclaration> SecurityProviders;

    public SecurityProvider(IEnumerable<ISecurityDeclaration> securityProviders)
    {
        SecurityProviders = securityProviders;
    }

    public IQueryable<Matrix> AllPermissions => allPermissions ??= SecurityProviders.SelectMany(p => p.Permissions()).AsQueryable();

    public IEnumerable<Matrix> Permissions<TEntity>(Action? action = null)
    {
        var resource = ResourceName<TEntity>();
        return Permissions(resource, action);
    }

    public IEnumerable<Matrix> Permissions(string resource, Action? action = null)
    {
        // get permission for resource
        var q = AllPermissions.Where(p => p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase));

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



