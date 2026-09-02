namespace Sencilla.Messaging;

/// <summary>
/// Lifecycle of a message in a durable stream, in the order a message travels it:
/// New → InProgress → Succeeded | Failed. A retryable failure goes back to New.
/// Fire-and-forget transports only ever see <see cref="New"/>.
///
/// These values are PERSISTED (dbo.Message.State). Append new ones at the end and never renumber —
/// reordering silently reinterprets every stored row.
/// </summary>
public enum MessageState
{
    /// <summary>Queued and claimable.</summary>
    New,

    /// <summary>Claimed by a worker and being handled; see ProcessService / ProcessStartDate.</summary>
    InProgress,

    /// <summary>Handled; terminal.</summary>
    Succeeded,

    /// <summary>Gave up — attempts exhausted, or a failure that could never succeed; terminal.</summary>
    Failed,
}
