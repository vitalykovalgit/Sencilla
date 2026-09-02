namespace Sencilla.Messaging;

/// <summary>
/// Raised in-process by <see cref="MessageStreamConsumer"/> when a message reaches its terminal
/// failed state — attempts exhausted, or a failure that can never succeed. Handle it with an
/// ordinary <c>IMessageHandler&lt;MessageFailed&lt;T&gt;&gt;</c> to surface the failure app-side
/// (flip a status, write a notification, raise an alert).
///
/// Fires exactly once per message, after the row is already terminal, and only for messages whose
/// payload type resolved — there is no T to raise for an unresolvable type, so those are logged and
/// left in the stream's failed state for an operator to see.
/// </summary>
/// <typeparam name="T">The payload type that failed.</typeparam>
public class MessageFailed<T>
{
    /// <summary>The message as it was delivered, envelope included.</summary>
    public required Message<T> Message { get; init; }

    /// <summary>Error CODE + detail, the same text written to the stream's row.</summary>
    public required string Error { get; init; }

    /// <summary>Attempts made before giving up.</summary>
    public int Attempts { get; init; }
}
