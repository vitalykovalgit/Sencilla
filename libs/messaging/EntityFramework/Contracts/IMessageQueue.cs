namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// Durable enqueue. Scoped on purpose: it writes through the caller's DbContext, so a message
/// inserted inside <c>repo.BeginTransaction()</c> commits or rolls back with everything else in
/// that transaction — the reason enqueueing does not go through <see cref="IMessageDispatcher"/>,
/// which is a singleton and cannot reach the caller's scope.
///
/// The [Message] table carries no user grants, so callers acting outside a request identity wrap
/// the enqueue in <c>Access.Root()</c> — their own endpoint or worker is the authorization.
/// </summary>
public interface IMessageQueue
{
    /// <summary>
    /// Enqueue a message with envelope fields the caller controls (EntityId, UserId, CorrelationId,
    /// AvailableAt for a delayed message). Returns the message id.
    /// </summary>
    Task<Guid> Enqueue<T>(Message<T> message, string stream, DateTime? availableAt = null, CancellationToken token = default);

    /// <summary>Enqueue a bare payload — no subject, no initiator, claimable immediately.</summary>
    Task<Guid> Enqueue<T>(T payload, string stream, CancellationToken token = default);
}
