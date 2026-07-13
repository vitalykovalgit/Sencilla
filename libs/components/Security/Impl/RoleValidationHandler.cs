namespace Sencilla.Component.Security;

/// <summary>
/// Hard validation of the role inheritance graph at write time (the graph is
/// admin-editable — fail loudly, not corrupt silently): parents must exist and
/// never close a cycle. Auto-registered by the event registrator.
/// <code>
/// // Owner(6) → Parent = Editor(7) → Parent = Viewer(8): ok
/// // Viewer(8) → Parent = Owner(6): BadRequestException — would close a cycle
/// // Any role → Parent = itself:    BadRequestException
/// </code>
/// </summary>
public class RoleValidationHandler :
      IEventHandlerBase<EntityCreatingEvent<Role>>
    , IEventHandlerBase<EntityUpdatingEvent<Role>>
{
    public Task HandleAsync(EntityCreatingEvent<Role> @event, IRoleClosure closure, CancellationToken token)
        => ValidateParents(@event?.Entities, closure);

    public Task HandleAsync(EntityUpdatingEvent<Role> @event, IRoleClosure closure, CancellationToken token)
        => ValidateParents(@event?.Entities, closure);

    private static Task ValidateParents(IQueryable<Role>? roles, IRoleClosure closure)
    {
        foreach (var role in roles ?? Enumerable.Empty<Role>().AsQueryable())
        {
            if (role.ParentId == null)
                continue;

            if (role.ParentId == role.Id)
                throw new BadRequestException($"Role '{role.Name}' cannot be its own parent.");

            if (!closure.Exists(role.ParentId.Value))
                throw new BadRequestException($"Parent role {role.ParentId} of '{role.Name}' does not exist.");

            if (closure.WouldCreateCycle(role.Id, role.ParentId))
                throw new BadRequestException($"Parent {role.ParentId} of role '{role.Name}' would create an inheritance cycle.");
        }

        return Task.CompletedTask;
    }
}
