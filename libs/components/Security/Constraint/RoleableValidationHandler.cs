namespace Sencilla.Component.Security;

/// <summary>
/// Hard validation on role-table writes: claims-derived roles are global-only.
/// Root is break-glass checked before any matrix lookup, and Anonymous/User are
/// granted to every (authenticated) caller automatically — a per-row holding of
/// any of them is dead data that LOOKS like a grant, so it is rejected loudly at
/// write time rather than left to mislead an audit.
///
/// Registered automatically by <see cref="SecureWithRegistrator"/> for every role
/// table named in a terminal [SecureWith] declaration. Every other role (Admin,
/// Owner, Editor, Viewer, custom) is holdable per-row — scope lives in WHERE a
/// role is held, not in the role itself:
/// <code>
/// new ProjectUser { UserId = u, EntityId = p, RoleId = (int)RoleType.Owner }  // ok
/// new ProjectUser { UserId = u, EntityId = p, RoleId = (int)RoleType.Admin }  // ok — admin of THIS project
/// new ProjectUser { UserId = u, EntityId = p, RoleId = (int)RoleType.Root }   // BadRequestException
/// </code>
/// </summary>
public class RoleableValidationHandler<TLink>
    : IEventHandlerBase<EntityCreatingEvent<TLink>>
    , IEventHandlerBase<EntityUpdatingEvent<TLink>>
    where TLink : class, IEntityRoleable
{
    private static readonly int[] GlobalOnly = [(int)RoleType.Root, (int)RoleType.Anonymous, (int)RoleType.User];

    public Task HandleAsync(EntityCreatingEvent<TLink> @event, CancellationToken token)
        => Validate(@event?.Entities);

    public Task HandleAsync(EntityUpdatingEvent<TLink> @event, CancellationToken token)
        => Validate(@event?.Entities);

    private static Task Validate(IQueryable<TLink>? links)
    {
        foreach (var link in links ?? Enumerable.Empty<TLink>().AsQueryable())
        {
            if (GlobalOnly.Contains(link.RoleId))
                throw new BadRequestException(
                    $"Role {link.RoleId} is claims-derived and global-only — it cannot be held on a specific row.");
        }

        return Task.CompletedTask;
    }
}
