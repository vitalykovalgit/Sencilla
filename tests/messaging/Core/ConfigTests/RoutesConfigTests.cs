namespace Sencilla.Messaging.Tests;

public class RoutesConfigTests
{
    private readonly RoutesConfig _routes = new();

    [Fact]
    public void GetRoute_NonExistentType_ReturnsNull()
    {
        var route = _routes.GetRoute<string>();

        Assert.Null(route);
    }

    [Fact]
    public void GetOrCreateRoute_CreatesNewRoute()
    {
        var route = _routes.GetOrCreateRoute<string>();

        Assert.NotNull(route);
        Assert.Equal(typeof(string), route.EntityType);
    }

    [Fact]
    public void GetOrCreateRoute_ReturnsSameInstance()
    {
        var route1 = _routes.GetOrCreateRoute<string>();
        var route2 = _routes.GetOrCreateRoute<string>();

        Assert.Same(route1, route2);
    }

    [Fact]
    public void GetRoute_AfterCreation_ReturnsRoute()
    {
        _routes.GetOrCreateRoute<int>();

        var route = _routes.GetRoute<int>();

        Assert.NotNull(route);
        Assert.Equal(typeof(int), route!.EntityType);
    }

    [Fact]
    public void GetRoute_ByType_ReturnsCorrectRoute()
    {
        _routes.GetOrCreateRoute(typeof(double));

        var route = _routes.GetRoute(typeof(double));

        Assert.NotNull(route);
    }

    [Fact]
    public void GetStreams_NoRoute_ReturnsEmpty()
    {
        var streams = _routes.GetStreams<string>();

        Assert.Empty(streams);
    }

    [Fact]
    public void GetStreams_WithRoute_ReturnsStreams()
    {
        _routes.GetOrCreateRoute<string>().ToStream("stream1", "stream2");

        var streams = _routes.GetStreams<string>();

        Assert.Equal(2, streams.Count());
        Assert.Contains("stream1", streams);
        Assert.Contains("stream2", streams);
    }

    [Fact]
    public void Send_CreatesRoute()
    {
        var route = _routes.Send<string>();

        Assert.NotNull(route);
        Assert.Equal(typeof(string), route.EntityType);
    }

    [Fact]
    public void Send_ByType_CreatesRoute()
    {
        var route = _routes.Send(typeof(int));

        Assert.NotNull(route);
        Assert.Equal(typeof(int), route.EntityType);
    }

    [Fact]
    public void SendToStream_RoutesMultipleTypesToStream()
    {
        _routes.SendToStream("my-stream", typeof(string), typeof(int));

        Assert.Contains("my-stream", _routes.GetStreams<string>());
        Assert.Contains("my-stream", _routes.GetStreams<int>());
    }

    [Fact]
    public void SendToStream_Generic_RoutesToMultipleStreams()
    {
        _routes.SendToStream<string>("stream-a", "stream-b");

        var streams = _routes.GetStreams<string>();
        Assert.Contains("stream-a", streams);
        Assert.Contains("stream-b", streams);
    }

    [Fact]
    public void GetOrCreateRoute_DifferentTypes_ReturnDifferentRoutes()
    {
        var route1 = _routes.GetOrCreateRoute<string>();
        var route2 = _routes.GetOrCreateRoute<int>();

        Assert.NotSame(route1, route2);
    }
}
