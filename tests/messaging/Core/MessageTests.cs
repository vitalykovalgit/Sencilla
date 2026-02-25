namespace Sencilla.Messaging.Tests;

public class MessageTests
{
    [Fact]
    public void Message_DefaultValues_AreCorrect()
    {
        var message = new Message();

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(Guid.Empty, message.CorrelationId);
        Assert.Equal(MessageType.Command, message.Type);
        Assert.Equal(MessageState.New, message.State);
        Assert.Null(message.Name);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.Namespace);
        Assert.Null(message.Error);
        Assert.True(message.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Message_Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-5);

        var message = new Message
        {
            Id = id,
            CorrelationId = correlationId,
            Type = MessageType.Event,
            State = MessageState.Succeeded,
            Name = "test-message",
            CreatedAt = createdAt,
            ProcessedAt = DateTime.UtcNow,
            Namespace = "Sencilla.Tests",
            Error = "test error"
        };

        Assert.Equal(id, message.Id);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal(MessageType.Event, message.Type);
        Assert.Equal(MessageState.Succeeded, message.State);
        Assert.Equal("test-message", message.Name);
        Assert.Equal(createdAt, message.CreatedAt);
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal("Sencilla.Tests", message.Namespace);
        Assert.Equal("test error", message.Error);
    }

    [Fact]
    public void Message_UniqueIds_AreGenerated()
    {
        var msg1 = new Message();
        var msg2 = new Message();

        Assert.NotEqual(msg1.Id, msg2.Id);
    }

    [Fact]
    public void GenericMessage_Payload_CanBeSet()
    {
        var message = new Message<string> { Payload = "hello" };

        Assert.Equal("hello", message.Payload);
        Assert.IsAssignableFrom<Message>(message);
    }

    [Fact]
    public void GenericMessage_DefaultPayload_IsNull()
    {
        var message = new Message<string>();

        Assert.Null(message.Payload);
    }

    [Fact]
    public void GenericMessage_ImplicitConversion_CreatesMessage()
    {
        Message<string> message = "hello";

        Assert.Equal("hello", message.Payload);
    }

    [Fact]
    public void GenericMessage_ImplicitConversion_WithComplexType()
    {
        var payload = new TestPayload { Name = "test", Value = 42 };
        Message<TestPayload> message = payload;

        Assert.Equal("test", message.Payload?.Name);
        Assert.Equal(42, message.Payload?.Value);
    }

    [Fact]
    public void GenericMessage_InheritsBaseProperties()
    {
        var message = new Message<int> { Payload = 42 };

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(MessageState.New, message.State);
        Assert.True(message.CreatedAt <= DateTime.UtcNow);
    }

    private class TestPayload
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
