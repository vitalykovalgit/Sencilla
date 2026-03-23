namespace Sencilla.Messaging;

public class RoutesConfig
{
    /// <summary>
    /// Contains the routing configurations for different message types.
    /// </summary>
    /// <returns></returns>
    private readonly ConcurrentDictionary<Type, RouteConfig> routings = new();

    /// <summary>
    /// Gets the routing configuration for the specified message type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public RouteConfig? GetRoute<T>() => GetRoute(typeof(T));
    public RouteConfig? GetRoute(Type type) => routings.ContainsKey(type) ? routings[type] : null;

    public RouteConfig GetOrCreateRoute<T>() => GetOrCreateRoute(typeof(T));
    public RouteConfig GetOrCreateRoute(Type type) => routings.GetOrAdd(type, _ => new RouteConfig(type));

    public IEnumerable<string> GetStreams<T>() => GetStreams(typeof(T));
    public IEnumerable<string> GetStreams(Type type) => GetRoute(type)?.Streams ?? Enumerable.Empty<string>();

    /// <summary>
    /// Gets the message route for the specified type.
    /// If the route does not exist, it will be created.
    /// </summary>
    public RouteConfig Send<T>() => GetOrCreateRoute<T>();
    public RouteConfig Send(Type type) => GetOrCreateRoute(type);

    /// <summary>
    /// Fluent alias for <see cref="Send{T}"/>. Begins a route definition for a single message type.
    /// </summary>
    /// <example>
    /// <code>r.Message&lt;MyMessage&gt;().To("queue1");</code>
    /// </example>
    public RouteConfig Message<T>() => GetOrCreateRoute<T>();

    /// <summary>
    /// Begins a route definition for multiple message types.
    /// All specified types will share the same route destinations.
    /// </summary>
    /// <example>
    /// <code>r.Messages(typeof(Msg1), typeof(Msg2)).To("queue1");</code>
    /// </example>
    public MultiRouteConfig Messages(params Type[] types)
    {
        var routes = types.Select(GetOrCreateRoute).ToList();
        return new MultiRouteConfig(routes);
    }

    /// <summary>
    /// Gets the message route for the specified type.
    /// If the route does not exist, it will be created.
    /// </summary>
    public void SendToStream(string stream, params Type[] types)
    {
        types.ForEach(type => GetOrCreateRoute(type).ToStream(stream));
    }

    public void SendToStream<T>(params IEnumerable<string> streams)
    {
        streams.ForEach(stream => GetOrCreateRoute<T>().ToStream(stream));
    }
}
