namespace Sencilla.Component.Security;

/// <summary>
/// Writes the creator's role row (UserId, EntityId, RoleId = the terminal
/// declaration's <see cref="SecureWithAttribute.CreatorRole"/>) when a user creates
/// a [SecureWith] terminal entity. Runs on the created event INSIDE the ambient
/// transaction, so the entity can never exist without its creator's role — a
/// roleless row would be permanently invisible under held-role constraints. The
/// role-row insert runs under Access.Root(): provisioning is a system action, not
/// subject to the caller's Create permission on the role table.
///
/// Registered automatically by <see cref="SecureWithRegistrator"/> for every
/// terminal declaration:
/// <code>
/// [SecureWith(typeof(ProjectUser))]                 // creator becomes Owner (6)
/// public class Project : IEntity&lt;Guid&gt; { }
///
/// [SecureWith(typeof(TeamUser), CreatorRole = 7)]   // creator becomes Editor
/// public class Team : IEntity&lt;int&gt; { }
/// </code>
/// The created-event post-image permission check composes lazily and evaluates
/// after ALL handlers ran, so the row this handler writes is already visible to it
/// regardless of handler ordering.
/// </summary>
public class CreatorRoleProvisioningHandler<TEntity, TEntityKey, TLink, TLinkKey>
    : IEventHandlerBase<EntityCreatedEvent<TEntity>>
    where TEntity : class, IEntity<TEntityKey>
    where TLink : class, IEntity<TLinkKey>, IEntityRoleable<TEntityKey>, new()
{
    public async Task HandleAsync(EntityCreatedEvent<TEntity> @event, ISystemVariable sysVars, ICreateRepository<TLink, TLinkKey> linkRepo, CancellationToken token)
    {
        if (@event?.Entities == null)
            return;

        // No identified user — nothing to provision (system/anonymous creates).
        var user = sysVars.GetCurrentUser();
        if (user == null || user.Id == Guid.Empty)
            return;

        var creatorRole = SecureWithChains.TryResolve(typeof(TEntity))!.CreatorRole;
        var entities = @event.Entities.AsEnumerable().ToList();

        // The link's EntityId must point at a real, saved entity. If a key is still
        // default here, the publisher did not hydrate it (e.g. a DB-generated key on a
        // raw Upsert/Merge bulk path) — provisioning against EntityId = 0/Guid.Empty
        // would grant the creator a role on a non-existent or unrelated row. Fail
        // loudly inside the transaction rather than write a misdirected grant.
        var defaultKey = default(TEntityKey);
        if (entities.Any(e => EqualityComparer<TEntityKey>.Default.Equals(e.Id, defaultKey!)))
            throw new InvalidOperationException(
                $"Cannot provision the creator role for {typeof(TEntity).Name}: its key was not populated before the created event. " +
                $"[SecureWith] terminal entities need an app-generated key or the plain Create() path (Upsert/Merge raw-bulk inserts do not read DB-generated keys back).");

        var links = entities
            .Select(e => new TLink { UserId = user.Id, EntityId = e.Id, RoleId = creatorRole })
            .ToList();

        if (links.Count == 0)
            return;

        using (Access.Root())
            await linkRepo.Create(links, token);
    }
}
