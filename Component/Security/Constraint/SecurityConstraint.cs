
namespace Sencilla.Component.Security;

public class SecurityConstraintHandler<TEntity>
    : IEventHandlerBase<EntityReadingEvent<TEntity>>
    , IEventHandlerBase<EntityCreatingEvent<TEntity>>
    , IEventHandlerBase<EntityUpdatingEvent<TEntity>>
    , IEventHandlerBase<EntityDeletingEvent<TEntity>>
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, (int)Action.Read, sysVars, matrixRepo);
    }

    public async Task HandleAsync(EntityCreatingEvent<TEntity> @event, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (@event?.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, (int)Action.Create, sysVars, matrixRepo);
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

    public async Task HandleAsync(EntityUpdatingEvent<TEntity> @event, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (@event?.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, (int)Action.Update, sysVars, matrixRepo);
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

    public async Task HandleAsync(EntityDeletingEvent<TEntity> @event, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, (int)Action.Delete, sysVars, matrixRepo);
    }

    protected async Task<IQueryable<TEntity>> ApplyConstraint(IQueryable<TEntity> query, int action, ISystemVariable sysVars, IReadRepository<Matrix> matrixRepo)
    {
        if (query == null)
            return query;

        // Check if access allowed then do nothing 
        // to avoid recursion 
        if (Access.Current?.AllowAll ?? false)
            return query;

        // current user
        var user = sysVars.GetCurrentUser();
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