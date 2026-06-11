
namespace Sencilla.Component.Security;

public class SecurityConstraintHandler<TEntity>
    : IEventHandlerBase<EntityReadingEvent<TEntity>>
    , IEventHandlerBase<EntityCreatingEvent<TEntity>>
    , IEventHandlerBase<EntityUpdatingEvent<TEntity>>
    , IEventHandlerBase<EntityDeletingEvent<TEntity>>
{
    private static readonly TimeSpan UserRolesCacheExpiration = TimeSpan.FromMinutes(5);

    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IMemoryCache cache, IReadRepository<UserRole, Guid> userRoles, CancellationToken token)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, Action.Read, sysVars, provider, cache, userRoles, token);
    }

    public async Task HandleAsync(EntityCreatingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IMemoryCache cache, IReadRepository<UserRole, Guid> userRoles, CancellationToken token)
    {
        if (@event?.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, Action.Create, sysVars, provider, cache, userRoles, token);
            var countBefore = @event.Entities.Count();
            var countAfter = safeEntities.Count();
            if (countBefore != countAfter)
            {
                // throw exeception
                // TODO: add detailed info
                //var badEntities = @event.Entities.Except(safeEntities);
                throw new ForbiddenException("Does not met criteria to create object");
            }
        }
    }

    public async Task HandleAsync(EntityUpdatingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IMemoryCache cache, IReadRepository<UserRole, Guid> userRoles, CancellationToken token)
    {
        if (@event?.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, Action.Update, sysVars, provider, cache, userRoles, token);
            var countBefore = @event.Entities.Count();
            var countAfter = safeEntities.Count();
            if (countBefore != countAfter)
            {
                // throw exeception
                // TODO: add detailed info
                //var badEntities = @event.Entities.Except(safeEntities);
                throw new ForbiddenException("Does not met criteria to update object");
            }
        }
    }

    public async Task HandleAsync(EntityDeletingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IMemoryCache cache, IReadRepository<UserRole, Guid> userRoles, CancellationToken token)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, Action.Delete, sysVars, provider, cache, userRoles, token);
    }

    protected async Task<IQueryable<TEntity>> ApplyConstraint(IQueryable<TEntity> query, Action action, ISystemVariable sysVars, ISecurityProvider provider, IMemoryCache cache, IReadRepository<UserRole, Guid> userRoles, CancellationToken token)
    {
        if (query == null)
            return query;

        // Check if access allowed then do nothing
        // to avoid recursion
        if (Access.Current?.AllowAll ?? false)
            return query;

        // Allow root access
        using (var rootAccess = Access.Root())
        {
            // retrieve permissions and current user
            var user = sysVars.GetCurrentUser();
            var permissions = await provider.Permissions<TEntity>(token, action); // DB, Attrs, FluenApi

            // Only rows of the current user's roles apply: claims baseline
            // (Anonymous always, User when authenticated) + persisted sec.UserRole
            // assignments (e.g. Admin), cached briefly.
            var roleIds = ResolveRoleIds(user, cache, userRoles);
            var applicable = permissions.Where(p => roleIds.Contains(p.RoleId)).ToList();

            // if operation is not allowed throw forbid exception
            if (applicable.Count == 0)
                throw new ForbiddenException();

            Expression<Func<TEntity, bool>>? constraints = null;
            var permissionsByRoles = applicable.GroupBy(p => p.RoleId);
            foreach (var group in permissionsByRoles)
            {
                // Same-role rows combine with AND; a row without a constraint
                // is an unconditional grant for that role.
                Expression<Func<TEntity, bool>>? roleExps = null;
                var unconstrained = false;
                foreach (var p in group)
                {
                    if (string.IsNullOrWhiteSpace(p.Constraint))
                    {
                        unconstrained = true;
                        continue;
                    }

                    var c = ParsedConstraintCache.Get(p.Constraint!);
                    var e = (Expression<Func<TEntity, bool>>)DynamicExpressionParser.ParseLambda(typeof(TEntity), typeof(bool), c.Constraint, c.Vars(sysVars));
                    roleExps = roleExps == null ? e : roleExps.AndAlso(e);
                }

                // A role granting the action without any constraint = full access.
                if (unconstrained && roleExps == null)
                    return query;

                // Different roles combine with OR.
                if (roleExps != null)
                    constraints = constraints == null ? roleExps : constraints.OrElse(roleExps);
            }

            if (constraints != null)
                query = query.Where(constraints);
        }

        return query;
    }

    /// <summary>
    /// Roles of the current user: Anonymous for everyone, User for any
    /// authenticated user, plus roles assigned in sec.UserRole (e.g. Admin).
    /// DB roles are cached briefly — assignments take effect within minutes.
    /// </summary>
    protected HashSet<int> ResolveRoleIds(User? user, IMemoryCache cache, IReadRepository<UserRole, Guid> userRoles)
    {
        var roleIds = new HashSet<int> { (int)RoleType.Anonymous };
        if (user == null)
            return roleIds;

        foreach (var role in user.Roles ?? [])
            roleIds.Add(role.RoleId);

        if (user.IsAnonymous() || user.Id == Guid.Empty)
            return roleIds;

        roleIds.Add((int)RoleType.User);

        var dbRoles = cache.GetOrCreate($"sec_user_roles_{user.Id}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = UserRolesCacheExpiration;
            // raw Query via Where — no entity events, no matrix recursion
            return userRoles.Where(r => r.UserId == user.Id).Select(r => r.RoleId).ToList();
        });

        foreach (var roleId in dbRoles ?? [])
            roleIds.Add(roleId);

        return roleIds;
    }
}
