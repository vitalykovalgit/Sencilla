namespace Sencilla.Component.Security;

/// <summary>
/// Contract and rationale in <see cref="IUserRoleResolver"/>.
///
/// The DB assignments are cached briefly and evicted by <see cref="SecurityCacheSignal"/>, so a role
/// granted or revoked in the admin takes effect within minutes rather than at next login — the same
/// latch <see cref="SecurityConstraintHandler{TEntity}"/> has always used, now shared.
/// </summary>
public class UserRoleResolver(
    ISystemVariable sysVars,
    IMemoryCache cache,
    IReadRepository<UserRole, Guid> userRoles,
    SecurityCacheSignal signal,
    IRoleClosure roleClosure) : IUserRoleResolver
{
    private static readonly TimeSpan UserRolesCacheExpiration = TimeSpan.FromMinutes(5);

    public HashSet<int> Current() => ResolveExpanded(sysVars.GetCurrentUser());

    public HashSet<int> ResolveExpanded(User? user) => roleClosure.Expand(Resolve(user));

    public HashSet<int> Resolve(User? user)
    {
        var roleIds = new HashSet<int> { (int)RoleType.Anonymous };
        if (user == null || user.IsAnonymous())
            return roleIds;

        foreach (var role in user.Roles ?? [])
            roleIds.Add(role.RoleId);

        // Any authenticated identity gets the User role — even before its DB record exists
        // (first-login self-registration), so the insert is authorised as User rather than Anonymous.
        roleIds.Add((int)RoleType.User);

        // Persisted role assignments (e.g. Admin in sec.UserRole) require a real Id.
        if (user.Id == Guid.Empty)
            return roleIds;

        var dbRoles = cache.GetOrCreate($"sec_user_roles_{user.Id}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = UserRolesCacheExpiration;
            entry.AddExpirationToken(signal.Token);
            // Raw Query via Where — no entity events, no matrix recursion.
            return userRoles.Where(r => r.UserId == user.Id).Select(r => r.RoleId).ToList();
        });

        foreach (var roleId in dbRoles ?? [])
            roleIds.Add(roleId);

        return roleIds;
    }
}
