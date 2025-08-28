namespace Sencilla.Messaging;

[DisableInjection]
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
    //public RouteConfig Send(params IEnumerable<Type> types) => types.ForEach(type => GetOrCreateRoute(type));

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
