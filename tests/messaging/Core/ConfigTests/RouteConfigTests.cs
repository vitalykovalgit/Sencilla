namespace Sencilla.Messaging.Tests;

public class RouteConfigTests
{
    [Fact]
    public void Constructor_SetsEntityType()
    {
        var route = new RouteConfig(typeof(string));

        Assert.Equal(typeof(string), route.EntityType);
    }

    [Fact]
    public void Streams_InitiallyEmpty()
    {
        var route = new RouteConfig(typeof(string));

        Assert.Empty(route.Streams);
    }

    [Fact]
    public void ToStream_AddsSingleStream()
    {
        var route = new RouteConfig(typeof(string));

        route.ToStream("stream1");

        Assert.Single(route.Streams);
        Assert.Contains("stream1", route.Streams);
    }

    [Fact]
    public void ToStream_AddsMultipleStreams()
    {
        var route = new RouteConfig(typeof(string));

        route.ToStream("stream1", "stream2", "stream3");

        Assert.Equal(3, route.Streams.Count());
    }

    [Fact]
    public void ToStream_DuplicateStream_DoesNotAddTwice()
    {
        var route = new RouteConfig(typeof(string));

        route.ToStream("stream1");
        route.ToStream("stream1");

        Assert.Single(route.Streams);
    }

    [Fact]
    public void To_AddsSingleStream()
    {
        var route = new RouteConfig(typeof(string));

        route.To("stream1");

        Assert.Single(route.Streams);
        Assert.Contains("stream1", route.Streams);
    }

    [Fact]
    public void To_AddsMultipleStreams()
    {
        var route = new RouteConfig(typeof(string));

        route.To("a", "b");

        Assert.Equal(2, route.Streams.Count());
    }

    [Fact]
    public void To_DuplicateStream_DoesNotAddTwice()
    {
        var route = new RouteConfig(typeof(string));

        route.To("stream1");
        route.To("stream1");

        Assert.Single(route.Streams);
    }

    [Fact]
    public void ToStream_And_To_CombineStreams()
    {
        var route = new RouteConfig(typeof(string));

        route.ToStream("a");
        route.To("b");

        Assert.Equal(2, route.Streams.Count());
        Assert.Contains("a", route.Streams);
        Assert.Contains("b", route.Streams);
    }
}
