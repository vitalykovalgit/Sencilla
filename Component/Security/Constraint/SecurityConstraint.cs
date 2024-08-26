
namespace Sencilla.Component.Security;

public class SecurityConstraintHandler<TEntity>
    : IEventHandlerBase<EntityReadingEvent<TEntity>>
    , IEventHandlerBase<EntityCreatingEvent<TEntity>>
    , IEventHandlerBase<EntityUpdatingEvent<TEntity>>
    , IEventHandlerBase<EntityDeletingEvent<TEntity>>
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, Action.Read, sysVars, provider);
    }

    public async Task HandleAsync(EntityCreatingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider)
    {
        if (@event?.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, Action.Create, sysVars, provider);
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

    public async Task HandleAsync(EntityUpdatingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider)
    {
        if (@event?.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, Action.Update, sysVars, provider);
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

    public async Task HandleAsync(EntityDeletingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, Action.Delete, sysVars, provider);
    }

    protected async Task<IQueryable<TEntity>> ApplyConstraint(IQueryable<TEntity> query, Action action, ISystemVariable sysVars, ISecurityProvider provider)
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
            // Get accesses 
            //var matrix = await matrixRepo.GetAll();
            //var accesses = matrix.Where(m =>
            //    (resource.Equals(m.Resource, StringComparison.OrdinalIgnoreCase)) &&
            //    (action & m.Action) == action &&
            //    (user.Roles.Any(r => r.RoleId == m.Role))
            //);

            //// if operation is not allowed throw forbid exception 
            //if (!accesses.Any())
            //    throw new ForbiddenException();

            //foreach (var a in accesses)
            //{
            //    // TODO: group by roles and add to predicate with 'or' clause 
            //    if (!string.IsNullOrWhiteSpace(a?.Constraint))
            //    {
            //        var c = ParsedConstraintCache.Get(a.Constraint);
            //        query = query.Where(c.Constraint, c.Vars(sysVars));
            //    }
            //}

            // retrieve permissions and current user
            var user = sysVars.GetCurrentUser();
            var permissions = provider.Permissions<TEntity>(action); // DB, Attrs, FluenApi

            // if operation is not allowed throw forbid exception 
            if (!permissions.Any())
                throw new ForbiddenException();

            Expression<Func<TEntity, bool>>? constraints = null;
            var permissionsByRoles = permissions.GroupBy(p => p.Role);
            foreach (var group in permissionsByRoles)
            {
                // 
                Expression<Func<TEntity, bool>>? roleExps = null;
                foreach (var p in group)
                {
                    if (string.IsNullOrWhiteSpace(p.Constraint))
                        continue;
                    
                    var c = ParsedConstraintCache.Get(p.Constraint());
                    var e = (Expression<Func<TEntity, bool>>)DynamicExpressionParser.ParseLambda(typeof(TEntity), typeof(bool), c.Constraint, c.Vars(sysVars));
                    roleExps = roleExps == null ? e : roleExps.AndAlso(e);
                }

                constraints = constraints == null ? roleExps : constraints.OrElse(roleExps);
            }

            if (constraints != null)
                query = query.Where(constraints);

            //// group multiple same role definitions by && 
            //// group multiple roles by || 
            //var constraint = permissions.GroupBy(p => p.Role)
            //                            .Select(p => $"({p.Select(p => $"({p.Constraint()})").Join(" && ")})")
            //                            .Join(" || ");

            //if (!string.IsNullOrEmpty(constraint))
            //{
            //    var c = ParsedConstraintCache.Get(constraint);
            //    query = query.Where(c.Constraint, c.Vars(sysVars));
            //}
        }

        return query;
    }
}