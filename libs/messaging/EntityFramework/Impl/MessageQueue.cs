namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// Writes the [Message] row through the caller's scoped repository — see <see cref="IMessageQueue"/>
/// for why this is not a dispatcher middleware.
/// </summary>
public class MessageQueue(ICreateRepository<AppMessage, Guid> messages) : IMessageQueue
{
    public async Task<Guid> Enqueue<T>(Message<T> message, string stream, DateTime? availableAt = null, CancellationToken token = default)
    {
        // Same stamping the dispatcher applies, so a message reaches a consumer identically
        // whichever side wrote it.
        message.PayloadType ??= PayloadTypeRegistry.KeyOf<T>();
        message.Namespace ??= typeof(T).FullName;
        message.Name ??= typeof(T).Name;

        var row = new AppMessage
        {
            Id = message.Id,
            Stream = stream,
            PayloadType = message.PayloadType!,
            Namespace = message.Namespace,
            Name = message.Name,
            CorrelationId = message.CorrelationId == Guid.Empty ? null : message.CorrelationId,
            State = (byte)MessageState.New,
            Payload = message.Payload is null ? null : JsonSerializer.Serialize(message.Payload, MessageJson.Options),
            Metadata = message.Metadata is null ? null : JsonSerializer.Serialize(message.Metadata, MessageJson.Options),
            EntityId = message.EntityId,
            UserId = message.UserId,
            AvailableAt = availableAt,
            CreatedAt = DateTime.UtcNow,
        };

        await messages.Create(row, token);
        return row.Id;
    }

    public Task<Guid> Enqueue<T>(T payload, string stream, CancellationToken token = default)
        => Enqueue(new Message<T> { Payload = payload }, stream, null, token);
}
