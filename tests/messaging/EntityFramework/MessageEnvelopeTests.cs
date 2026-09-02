using System.Text.Json;

namespace Sencilla.Messaging.EntityFramework.Tests;

/// <summary>
/// The row → wire → typed-message round trip. Casing lives here: the envelope is written camelCase
/// and the consumer reads with <see cref="MessageJson"/>, so a mismatch would silently produce a
/// message with every field null rather than an error.
/// </summary>
public class MessageEnvelopeTests
{
    [Fact]
    public void From_RoundTripsThroughTheConsumerDeserializationPath()
    {
        var id = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var row = new QueueMessage
        {
            Id = id,
            Stream = "tasks",
            PayloadType = PayloadTypeRegistry.KeyOf<SamplePayload>(),
            Namespace = typeof(SamplePayload).FullName,
            Name = nameof(SamplePayload),
            State = (byte)MessageState.InProgress,
            Payload = JsonSerializer.Serialize(new SamplePayload { Value = "hello", Count = 7 }, MessageJson.Options),
            Metadata = JsonSerializer.Serialize(new Dictionary<string, string> { ["origin"] = "test" }, MessageJson.Options),
            EntityId = entityId,
            UserId = userId,
            Attempts = 2,
            CreatedAt = new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = [],
        };

        var json = MessageEnvelope.From(row);

        // Exactly what MessageStreamConsumer does: untyped envelope first, then the typed message.
        var envelope = JsonSerializer.Deserialize<Message>(json, MessageJson.Options)!;
        Assert.Equal(id, envelope.Id);
        Assert.Equal(PayloadTypeRegistry.KeyOf<SamplePayload>(), envelope.PayloadType);
        Assert.Equal(2, envelope.Attempts);
        Assert.Equal(entityId, envelope.EntityId);
        Assert.Equal(userId, envelope.UserId);
        Assert.Equal("origin", Assert.Single(envelope.Metadata!).Key);

        var typed = JsonSerializer.Deserialize<Message<SamplePayload>>(json, MessageJson.Options)!;
        Assert.Equal("hello", typed.Payload!.Value);
        Assert.Equal(7, typed.Payload.Count);
    }

    [Fact]
    public void From_NullPayloadAndMetadata_StillProducesAReadableEnvelope()
    {
        var row = new QueueMessage
        {
            Id = Guid.NewGuid(),
            Stream = "tasks",
            PayloadType = "some.type",
            CreatedAt = DateTime.UtcNow,
            RowVersion = [],
        };

        var envelope = JsonSerializer.Deserialize<Message>(MessageEnvelope.From(row), MessageJson.Options)!;

        Assert.Equal("some.type", envelope.PayloadType);
        Assert.Null(envelope.Metadata);
    }

    public class SamplePayload
    {
        public string Value { get; set; } = "";
        public int Count { get; set; }
    }
}
