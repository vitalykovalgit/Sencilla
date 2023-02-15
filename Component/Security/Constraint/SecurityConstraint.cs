
namespace Sencilla.Component.Security;

public class SecurityConstraintHandler<TEntity>
    : IEventHandlerBase<EntityReadingEvent<TEntity>>
    //, IEventHandlerBase<EntityCreatingEvent<TEntity>>
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, (int)Action.Read, sysVars, matrixRepo);
    }

    //public async Task HandleAsync(EntityCreatingEvent<TEntity> @event, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    //{
    //    if (@event != null)
    //        @event.Entities = await ApplyConstraint(@event.Entities, (int)Action.Create, sysVars, matrixRepo);
    //}

    protected async Task<IQueryable<TEntity>> ApplyConstraint(IQueryable<TEntity> query, int action, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (query == null)
            return query;

        // Check if access allowed then do nothing 
        if (Access.Current?.AllowAll ?? false)
            return query;

        // current user
        var user = sysVars.GetCurrentUser();// UserProvider.CurrentUser; // get current user and his roles
        var resource = ResourceName<TEntity>(); // resource name or all

        // Allow root access 
        using (var rootAccess = Access.Root())
        {
            // Get accesses 
            var matrix = await matrixRepo.GetAll();
            var accesses = matrix.Where(m =>
                (resource.Equals(m.Resource, StringComparison.OrdinalIgnoreCase)) &&
                (action & m.Action) == action &&
                (user.Roles.Any(r => r.RoleId == m.Role))
            );

            // if operation is not allowed throw forbid exception 
            if (!accesses.Any())
                throw new ForbiddenException();

            foreach (var a in accesses)
            {
                // TODO: group by roles and add to predicate with 'or' clause 
                if (!string.IsNullOrWhiteSpace(a?.Constraint))
                {
                    var c = ParsedConstraintCache.Get(a.Constraint);
                    query = query.Where(c.Constraint, c.Vars(sysVars));
                }
            }
        }

        return query;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string ResourceName<TEntity>()
    {
        // try to get attribute 
        var entityType = typeof(TEntity);
        var resourceAttr = entityType.GetCustomAttribute<ResourceAttribute>();
        return resourceAttr?.Name ?? entityType.Name;
    }

}

/*
public class SecurityConstraint : Resolveable, IReadConstraint
{
    ICurrentUserProvider UserProvider;

    public SecurityConstraint(ICurrentUserProvider currentUserProvider, IResolver resolver): base(resolver)
    { 
        UserProvider = currentUserProvider;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string ResourceName<TEntity>()
    {
        // try to get attribute 
        var entityType = typeof(TEntity);
        var resourceAttr = entityType.GetCustomAttribute<ResourceAttribute>();
        return resourceAttr?.Name ?? entityType.Name; 
    }

    public async Task<IQueryable<TEntity>> Apply<TEntity>(IQueryable<TEntity> query, int action, IFilter? filter = null)
    {
        // Check if access allowed then do nothing 
        if (Access.Current?.AllowAll ?? false)
            return query;

        // current user
        var user  = UserProvider.CurrentUser; // get current user and his roles
        var roles = user.Roles;
        //var action = (int)Action.Read;
        var resource = ResourceName<TEntity>(); // resource name or all

        // Allow root access 
        using (var rootAccess = Access.Root())
        {
            // Get accesses 
            var matrix = await R<IReadRepository<Matrix>>().GetAll();
            var accesses = matrix.Where(m =>
                (resource.Equals(m.Resource, StringComparison.OrdinalIgnoreCase)) &&
                (action & m.Action) == action &&
                (roles.Any(r => r.RoleId == m.Role))
            );

            // if operation is not allowed throw forbid exception 
            if (!accesses.Any())
                throw new ForbiddenException();

            // Apply constraints 
            var sysVars = R<ISystemVariable>();
            
            foreach (var a in accesses)
            {
                // TODO: group by roles and add to predicate with 'or' clause 
                
                if (!string.IsNullOrWhiteSpace(a?.Constraint))
                {
                    var c = ParsedConstraintCache.Get(a.Constraint);
                    query = query.Where(c.Constraint, c.Vars(sysVars));
                }
            }
        }
        return query;
    }
}
//*/