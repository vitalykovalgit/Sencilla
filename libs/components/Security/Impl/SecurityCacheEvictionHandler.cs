namespace Sencilla.Component.Security;

/// <summary>
/// Evicts the security caches when a permission-source entity changes — the security
/// system dogfoods the entity-event pipeline it protects. Auto-registered by the
/// event registrator; runs inside the writer's ambient transaction (pre-commit),
/// which the short cache TTL backstops.
/// </summary>
public class SecurityCacheEvictionHandler :
      IEventHandlerBase<EntityCreatedEvent<Matrix>>
    , IEventHandlerBase<EntityUpdatedEvent<Matrix>>
    , IEventHandlerBase<EntityDeletedEvent<Matrix>>
    , IEventHandlerBase<EntityCreatedEvent<Role>>
    , IEventHandlerBase<EntityUpdatedEvent<Role>>
    , IEventHandlerBase<EntityDeletedEvent<Role>>
    , IEventHandlerBase<EntityCreatedEvent<UserRole>>
    , IEventHandlerBase<EntityUpdatedEvent<UserRole>>
    , IEventHandlerBase<EntityDeletedEvent<UserRole>>
{
    public Task HandleAsync(EntityCreatedEvent<Matrix> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);
    public Task HandleAsync(EntityUpdatedEvent<Matrix> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);
    public Task HandleAsync(EntityDeletedEvent<Matrix> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);

    public Task HandleAsync(EntityCreatedEvent<Role> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);
    public Task HandleAsync(EntityUpdatedEvent<Role> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);
    public Task HandleAsync(EntityDeletedEvent<Role> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);

    public Task HandleAsync(EntityCreatedEvent<UserRole> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);
    public Task HandleAsync(EntityUpdatedEvent<UserRole> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);
    public Task HandleAsync(EntityDeletedEvent<UserRole> @event, SecurityCacheSignal signal, CancellationToken token) => Invalidate(signal);

    private static Task Invalidate(SecurityCacheSignal signal)
    {
        signal.Invalidate();
        return Task.CompletedTask;
    }
}
