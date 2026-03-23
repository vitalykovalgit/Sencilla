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

    // ── Message<T>() alias ──

    [Fact]
    public void Message_IsAliasForGetOrCreateRoute()
    {
        var route = _routes.Message<string>();

        Assert.NotNull(route);
        Assert.Equal(typeof(string), route.EntityType);
    }

    [Fact]
    public void Message_ReturnsSameInstanceAsGetOrCreateRoute()
    {
        var route1 = _routes.Message<string>();
        var route2 = _routes.GetOrCreateRoute<string>();

        Assert.Same(route1, route2);
    }

    [Fact]
    public void Message_CanChainWithTo()
    {
        _routes.Message<string>().To("queue1");

        var streams = _routes.GetStreams<string>();
        Assert.Contains("queue1", streams);
    }

    // ── Messages(params Type[]) ──

    [Fact]
    public void Messages_ReturnsMultiRouteConfig()
    {
        var multi = _routes.Messages(typeof(string), typeof(int));

        Assert.NotNull(multi);
    }

    [Fact]
    public void Messages_To_RoutesAllTypesToStream()
    {
        _routes.Messages(typeof(string), typeof(int)).To("shared-queue");

        Assert.Contains("shared-queue", _routes.GetStreams<string>());
        Assert.Contains("shared-queue", _routes.GetStreams<int>());
    }

    [Fact]
    public void Messages_To_MultipleStreams()
    {
        _routes.Messages(typeof(string), typeof(int)).To("q1", "q2");

        Assert.Contains("q1", _routes.GetStreams<string>());
        Assert.Contains("q2", _routes.GetStreams<string>());
        Assert.Contains("q1", _routes.GetStreams<int>());
        Assert.Contains("q2", _routes.GetStreams<int>());
    }

    [Fact]
    public void Messages_ToStream_RoutesAllTypesToStream()
    {
        _routes.Messages(typeof(string), typeof(int)).ToStream("shared-stream");

        Assert.Contains("shared-stream", _routes.GetStreams<string>());
        Assert.Contains("shared-stream", _routes.GetStreams<int>());
    }

    [Fact]
    public void Messages_ToStream_MultipleStreams()
    {
        _routes.Messages(typeof(string), typeof(int)).ToStream("s1", "s2");

        Assert.Contains("s1", _routes.GetStreams<string>());
        Assert.Contains("s2", _routes.GetStreams<string>());
        Assert.Contains("s1", _routes.GetStreams<int>());
        Assert.Contains("s2", _routes.GetStreams<int>());
    }

    [Fact]
    public void Messages_SingleType_WorksLikeMessage()
    {
        _routes.Messages(typeof(string)).To("queue1");

        Assert.Contains("queue1", _routes.GetStreams<string>());
    }
}
