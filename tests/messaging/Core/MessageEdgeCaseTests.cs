namespace Sencilla.Messaging.Tests;

public class MessageEdgeCaseTests
{
    [Fact]
    public void Message_CreatedAt_IsUtcNow()
    {
        var before = DateTime.UtcNow;
        var message = new Message();
        var after = DateTime.UtcNow;

        Assert.InRange(message.CreatedAt, before, after);
    }

    [Fact]
    public void Message_Id_IsNotEmpty()
    {
        var message = new Message();

        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Message_CorrelationId_DefaultsToEmpty()
    {
        var message = new Message();

        Assert.Equal(Guid.Empty, message.CorrelationId);
    }

    [Fact]
    public void Message_Type_DefaultsToCommand()
    {
        var message = new Message();

        Assert.Equal(MessageType.Command, message.Type);
    }

    [Fact]
    public void Message_AllMessageTypes_CanBeAssigned()
    {
        foreach (var type in Enum.GetValues<MessageType>())
        {
            var message = new Message { Type = type };
            Assert.Equal(type, message.Type);
        }
    }

    [Fact]
    public void Message_AllMessageStates_CanBeAssigned()
    {
        foreach (var state in Enum.GetValues<MessageState>())
        {
            var message = new Message { State = state };
            Assert.Equal(state, message.State);
        }
    }

    [Fact]
    public void GenericMessage_ImplicitConversion_NullPayload()
    {
        string? payload = null;
        Message<string?> message = payload;

        Assert.Null(message.Payload);
    }

    [Fact]
    public void GenericMessage_ImplicitConversion_ValueType()
    {
        Message<int> message = 0;

        Assert.Equal(0, message.Payload);
    }

    [Fact]
    public void GenericMessage_ImplicitConversion_PreservesBaseProperties()
    {
        Message<string> message = "test";

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(MessageState.New, message.State);
        Assert.Equal(MessageType.Command, message.Type);
    }

    [Fact]
    public void GenericMessage_WithNestedGenericPayload()
    {
        var payload = new List<Dictionary<string, int>> { new() { ["key"] = 42 } };
        var message = new Message<List<Dictionary<string, int>>> { Payload = payload };

        Assert.NotNull(message.Payload);
        Assert.Single(message.Payload!);
        Assert.Equal(42, message.Payload![0]["key"]);
    }

    [Fact]
    public void GenericMessage_WithValueTypePayload_Default()
    {
        var message = new Message<int>();

        Assert.Equal(0, message.Payload);
    }

    [Fact]
    public void Message_ManyInstances_AllHaveUniqueIds()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => new Message().Id).ToHashSet();

        Assert.Equal(100, ids.Count);
    }

    [Fact]
    public void GenericMessage_ErrorAndState_CanBeModified()
    {
        var message = new Message<string> { Payload = "test" };

        message.State = MessageState.Failed;
        message.Error = "something went wrong";

        Assert.Equal(MessageState.Failed, message.State);
        Assert.Equal("something went wrong", message.Error);
    }

    [Fact]
    public void GenericMessage_ProcessedAt_CanBeSet()
    {
        var message = new Message<string> { Payload = "test" };
        var now = DateTime.UtcNow;

        message.ProcessedAt = now;

        Assert.Equal(now, message.ProcessedAt);
    }
}
