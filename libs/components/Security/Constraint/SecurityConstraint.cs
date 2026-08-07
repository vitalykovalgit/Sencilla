using Microsoft.Extensions.Logging;

namespace Sencilla.Component.Security;

/// <summary>
/// Composes permission enforcement into entity queries at every lifecycle point.
/// A matrix row (Role, Resource, Action, Constraint) activates when the caller
/// holds its role GLOBALLY (claims + sec.UserRole, expanded by inheritance) or
/// ON-THE-ROW (a [SecureWith] role table row, matched by the reverse closure) —
/// scope lives in where a role is held, not in the role itself. Grants accumulate:
/// any global grant without a constraint is a full grant; everything else ORs.
/// The row's Constraint column ANDs WITHIN a held grant (business condition):
/// <code>
/// (User,   'project',     Create, NULL)                 // global: any authenticated user
/// (Viewer, 'project',     Read,   NULL)                 // held: Viewer/Editor/Owner on the row
/// (Editor, 'projectitem', Update, 'Project.Status != 2')// held on parent AND condition
/// (Admin,  'project',     Read,   NULL)                 // global Admin — or Admin held on one row
/// </code>
/// Enforcement points: reads/deletes narrow the query; updates check pre- and
/// post-image against DB state; creates gate applicability up front and enforce
/// constraints on the POST-image inside the transaction (after role provisioning),
/// because in-memory objects cannot answer held-role or navigation constraints.
/// </summary>
public class SecurityConstraintHandler<TEntity>
    : IEventHandlerBase<EntityReadingEvent<TEntity>>
    , IEventHandlerBase<EntityCreatingEvent<TEntity>>
    , IEventHandlerBase<EntityCreatedEvent<TEntity>>
    , IEventHandlerBase<EntityUpdatingEvent<TEntity>>
    , IEventHandlerBase<EntityUpdatedEvent<TEntity>>
    , IEventHandlerBase<EntityDeletingEvent<TEntity>>
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, Action.Read, sysVars, provider, roleResolver, roleClosure, codeRules, services, token);
    }

    public async Task HandleAsync(EntityCreatingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token)
    {
        if (@event?.Entities == null)
            return;

        // Applicability gate only — throws Forbidden when no grant source could ever
        // authorize this create. Constraint EVALUATION is deferred to the post-image
        // check on the created event: the in-memory objects here cannot answer
        // held-role or navigation constraints (role rows and parents live in the DB),
        // and client-supplied values must not be trusted anyway.
        await ApplyConstraint(@event.Entities, Action.Create, sysVars, provider, roleResolver, roleClosure, codeRules, services, token);
    }

    public async Task HandleAsync(EntityCreatedEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token)
    {
        if (@event == null)
            return;

        // When the created entity is its own [SecureWith] role table, the rows being
        // inserted must NOT count as the holding that authorizes their own creation
        // (else any user writes themselves an Owner row and it self-approves). Exclude
        // the inserted keys so the held check requires a PRE-existing holding.
        var excludeSelfKeys = SelfSecuringKeys(@event.Entities);

        // Post-image: constraints composed into a DB query over the just-inserted
        // rows, inside the ambient transaction. The narrowing is evaluated by the
        // repository AFTER all created-handlers ran, so the creator's provisioned
        // role row is visible regardless of handler order; the repository rolls the
        // insert back when the narrowed query loses rows.
        if (@event.DbEntities != null)
        {
            @event.DbEntities = await ApplyConstraint(@event.DbEntities, Action.Create, sysVars, provider, roleResolver, roleClosure, codeRules, services, token, excludeSelfKeys);
            return;
        }

        // Publisher supplied no database query (non-EF path): fall back to
        // validating the in-memory set. Held-role grants still evaluate correctly
        // (the role-table subquery executes per row); navigation-property
        // constraints cannot and remain an EF-only feature.
        if (@event.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, Action.Create, sysVars, provider, roleResolver, roleClosure, codeRules, services, token, excludeSelfKeys);
            if (@event.Entities.Count() != safeEntities.Count())
                throw new ForbiddenException("Does not met criteria to create object");
        }
    }

    /// <summary>
    /// The keys of the created rows when <typeparamref name="TEntity"/> is its own
    /// [SecureWith] role table (self-securing); null otherwise. Used to exclude the
    /// in-flight rows from their own held-Create authorization.
    /// </summary>
    private static IReadOnlyCollection<object>? SelfSecuringKeys(IQueryable<TEntity>? entities)
    {
        if (entities == null || SecureWithChains.TryResolve(typeof(TEntity))?.LinkType != typeof(TEntity))
            return null;

        var id = typeof(TEntity).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return id == null ? null : entities.AsEnumerable().Select(e => id.GetValue(e)!).ToList();
    }

    public async Task HandleAsync(EntityUpdatingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token)
    {
        if (@event == null)
            return;

        // Pre-image: compose the constraint into the database query — the DB state
        // decides, never the client-supplied objects (they can be forged to satisfy
        // any rule while targeting someone else's row). The repository denies the
        // batch when the narrowed query loses existing rows.
        if (@event.DbEntities != null)
        {
            @event.DbEntities = await ApplyConstraint(@event.DbEntities, Action.Update, sysVars, provider, roleResolver, roleClosure, codeRules, services, token);
            return;
        }

        // Publisher supplied no database query (non-EF path): fall back to
        // validating the in-memory set.
        if (@event.Entities != null)
        {
            var safeEntities = await ApplyConstraint(@event.Entities, Action.Update, sysVars, provider, roleResolver, roleClosure, codeRules, services, token);
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

    public async Task HandleAsync(EntityUpdatedEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token)
    {
        // Post-image: the same constraint must still hold on the rows after the write
        // (a row may not be moved out of the writer's own permission scope — including
        // re-parenting under a [SecureWith] hop). Runs inside the ambient transaction —
        // the repository rolls the write back when the narrowed query loses rows.
        if (@event?.DbEntities != null)
            @event.DbEntities = await ApplyConstraint(@event.DbEntities, Action.Update, sysVars, provider, roleResolver, roleClosure, codeRules, services, token);
    }

    public async Task HandleAsync(EntityDeletingEvent<TEntity> @event, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token)
    {
        if (@event != null)
            @event.Entities = await ApplyConstraint(@event.Entities, Action.Delete, sysVars, provider, roleResolver, roleClosure, codeRules, services, token);
    }

    protected async Task<IQueryable<TEntity>> ApplyConstraint(IQueryable<TEntity> query, Action action, ISystemVariable sysVars, ISecurityProvider provider, IUserRoleResolver roleResolver, IRoleClosure roleClosure, IEnumerable<IPermissionRule<TEntity>> codeRules, IServiceProvider services, CancellationToken token, IReadOnlyCollection<object>? excludeSelfKeys = null)
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
            // Only rows of the current user's roles apply: claims baseline
            // (Anonymous always, User when authenticated) + persisted sec.UserRole
            // assignments (e.g. Admin), cached briefly.
            var user = sysVars.GetCurrentUser();
            var roleIds = roleResolver.Resolve(user);

            // Root is break-glass: hard-coded bypass before any matrix lookup —
            // immune to matrix misconfiguration, cache staleness and admin lockout.
            if (roleIds.Contains((int)RoleType.Root))
                return query;

            // Inheritance: a role also grants everything its ancestors grant
            // (ParentId chain, cached).
            roleIds = roleClosure.Expand(roleIds);

            var permissions = await provider.Permissions<TEntity>(token, action); // DB, Attrs, FluenApi

            // Unified activation: rows whose role the caller holds globally apply as
            // before; the rest MAY apply by on-the-row holding when the entity has a
            // [SecureWith] chain and the caller is an identified user.
            var globalRows = new List<Matrix>();
            var heldRows = new List<Matrix>();
            foreach (var p in permissions)
                (roleIds.Contains(p.RoleId) ? globalRows : heldRows).Add(p);

            var chain = SecureWithChains.TryResolve(typeof(TEntity));

            // Creating a TERMINAL entity provisions the creator's own role row in the
            // same transaction — a held grant for Create would be satisfied BY the very
            // create it is supposed to authorize (circular). Root-entity Create is
            // global-grant-only; held Create stays meaningful on hop entities, where it
            // checks a PRE-existing holding on the parent.
            if (action == Action.Create && chain is { Hops.Count: 0 } && heldRows.Count > 0)
            {
                services.GetService<ILogger<SecurityConstraintHandler<TEntity>>>()?.LogWarning(
                    "Matrix rows granting Create on '{Resource}' by held role are inert: {Entity} is a [SecureWith] terminal, and creating it provisions the creator's role. Grant Create to a global role instead.",
                    heldRows[0].Resource, typeof(TEntity).Name);
                heldRows.Clear();
            }

            var canHold = user != null && user.Id != Guid.Empty && chain != null;
            if (!canHold)
                heldRows.Clear();

            // Code-declared rules (specification pattern) are one more grant
            // source — union with matrix rows. Global holding only.
            var applicableRules = codeRules
                .Where(r => roleIds.Contains(r.RoleId) && ((int)r.Action & (int)action) == (int)action)
                .Select(r => r.Constraint(sysVars))
                .ToList();

            // if operation is not allowed throw forbid exception. Held rows count as
            // applicable for an identified user — no holdings then means an empty
            // result, not a 403 (anonymous callers cannot hold, so they still 403).
            if (globalRows.Count == 0 && heldRows.Count == 0 && applicableRules.Count == 0)
                throw new ForbiddenException();

            // Grants accumulate: any GLOBAL grant without a constraint is a full
            // grant, and everything else combines with OR — adding a rule can only
            // widen access. A held grant is never unconditional (the holding itself
            // is its condition); its Constraint column ANDs within the grant.
            if (globalRows.Any(p => string.IsNullOrWhiteSpace(p.Constraint)) || applicableRules.Any(e => e == null))
                return query;

            // OrElse/AndAlso are no-ops on a null receiver — seed explicitly.
            Expression<Func<TEntity, bool>>? constraints = null;
            foreach (var p in globalRows)
            {
                var e = Parse(p.Constraint!, sysVars);
                constraints = constraints == null ? e : constraints.OrElse(e);
            }

            foreach (var p in heldRows)
            {
                var held = SecureWithFilter.Build<TEntity>(p.RoleId, user!.Id, services, roleClosure, excludeSelfKeys)!;
                if (!string.IsNullOrWhiteSpace(p.Constraint))
                    held = held.AndAlso(Parse(p.Constraint!, sysVars))!;

                constraints = constraints == null ? held : constraints.OrElse(held);
            }

            foreach (var e in applicableRules)
                constraints = constraints == null ? e! : constraints.OrElse(e!);

            // Fail closed: applicability was established above, so a null here would
            // be a composition bug — never fall through to an unfiltered query.
            if (constraints == null)
                throw new ForbiddenException();

            query = query.Where(constraints);
        }

        return query;
    }

    private static Expression<Func<TEntity, bool>> Parse(string constraint, ISystemVariable sysVars)
    {
        var c = ParsedConstraintCache.Get(constraint);
        return (Expression<Func<TEntity, bool>>)DynamicExpressionParser.ParseLambda(typeof(TEntity), typeof(bool), c.Constraint, c.Vars(sysVars));
    }

}
