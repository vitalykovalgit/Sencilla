namespace Sencilla.Messaging.Tests;

public class StreamAttributeTests
{
    [Fact]
    public void Constructor_SingleName_SetsNames()
    {
        var attr = new StreamAttribute("stream1");

        Assert.Single(attr.Names);
        Assert.Contains("stream1", attr.Names);
    }

    [Fact]
    public void Constructor_MultipleNames_SetsNames()
    {
        var attr = new StreamAttribute("stream1", "stream2", "stream3");

        Assert.Equal(3, attr.Names.Count());
    }

    [Fact]
    public void Constructor_NoNames_EmptyCollection()
    {
        var attr = new StreamAttribute();

        Assert.Empty(attr.Names);
    }

    [Fact]
    public void Attribute_CanBeRetrievedFromClass()
    {
        var attrs = typeof(DecoratedMessage).GetCustomAttributes(typeof(StreamAttribute), true);

        Assert.Single(attrs);
        var streamAttr = (StreamAttribute)attrs[0];
        Assert.Contains("my-stream", streamAttr.Names);
    }

    [Stream("my-stream")]
    private class DecoratedMessage { }
}

public class ExtendDispatcherAttributeTests
{
    [Fact]
    public void Method_DefaultIsNull()
    {
        var attr = new ExtendDispatcherAttribute();

        Assert.Null(attr.Method);
    }

    [Fact]
    public void Method_CanBeSet()
    {
        var attr = new ExtendDispatcherAttribute { Method = "CustomSend" };

        Assert.Equal("CustomSend", attr.Method);
    }

    [Fact]
    public void Attribute_CanBeRetrievedFromClass()
    {
        var attrs = typeof(DecoratedCommand).GetCustomAttributes(typeof(ExtendDispatcherAttribute), true);

        Assert.Single(attrs);
    }

    [ExtendDispatcher(Method = "SendCustom")]
    private class DecoratedCommand { }
}

public class PayloadTypeAttributeTests
{
    [Fact]
    public void Constructor_SetsName()
    {
        var attr = new PayloadTypeAttribute("my-topic");

        Assert.Equal("my-topic", attr.Name);
    }

    [Fact]
    public void Attribute_CanBeRetrievedFromClass()
    {
        var attrs = typeof(DecoratedPayload).GetCustomAttributes(typeof(PayloadTypeAttribute), true);

        Assert.Single(attrs);
        var payloadAttr = (PayloadTypeAttribute)attrs[0];
        Assert.Equal("test-topic", payloadAttr.Name);
    }

    [PayloadType("test-topic")]
    private class DecoratedPayload { }
}
