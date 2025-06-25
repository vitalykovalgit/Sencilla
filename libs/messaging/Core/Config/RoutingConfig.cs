namespace Sencilla.Messaging;

public class RoutingConfig 
{
    private readonly ConcurrentDictionary<Type, RouteConfig> routings = new();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public RouteConfig GetRoute<T>() => GetRoute(typeof(T));
    public RouteConfig GetRoute(Type type) => routings.GetOrAdd(type, _ => new RouteConfig { EntityType = type });

    /// <summary>
    /// Gets the message route for the specified type.  
    /// If the route does not exist, it will be created.
    /// </summary>   
    public RouteConfig Send<T>() => GetRoute<T>();
    public RouteConfig Send(Type type) => GetRoute(type);

    /// <summary>
    /// Gets the message route for the specified type.  
    /// If the route does not exist, it will be created.
    /// </summary>
    public void SendToQueue(string queue, params Type[] types)
    {
        types.ForEach(type => GetRoute(type).ToQueue(queue));
    }
}
