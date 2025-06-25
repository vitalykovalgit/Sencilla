namespace Sencilla.Messaging;

public class ProviderConfig(IServiceCollection services)
{
    public string Name { get; set; } = string.Empty;

    public IServiceCollection Services { get; private set; } = services;

    /// <summary>
    /// A list of middleware types to be used in the messaging pipeline.
    /// </summary>
    public List<Type>? Middlewares { get; private set; }

    /// <summary>
    /// Configuration for routing messages to specific handlers or endpoints.
    /// </summary>
    public RoutingConfig Routing { get; private set; } = new();

    /// <summary>
    /// 
    /// </summary>
    public ConsumersConfig? Consumers { get; private set; }

    /// <summary>
    /// A list of queues to be consumed by this provider.
    /// Each queue can have its own configuration, such as concurrency settings and prefetch counts.
    /// </summary>
    public ConcurrentDictionary<string, QueueConfig> Streams { get; private set; } = [];
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public ProviderConfig AddRouting(Action<RoutingConfig> config)
    {
        config(Routing);
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ProviderConfig AddMiddleware<T>() where T : class, IMessageMiddleware
    {
        Middlewares?.Add(typeof(T));
        Services.AddSingleton<T>();
        return this;
    }

    public virtual ProviderConfig AddConsumers(Action<ConsumersConfig> config)
    {
        Consumers ??= new ConsumersConfig();
        config(Consumers);

        // Add Host Services

        return this;
    }

    internal void Cleanup()
    {
        Services = null!; // Clear the services collection to prevent further modifications
    }
}

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class MessagingConfig(IServiceCollection services) : ProviderConfig(services)
{
    public ConcurrentDictionary<string, ProviderConfig> Providers { get; } = new();

    public MessagingConfig AddProvider(ProviderConfig provider)
    {
        Providers[provider.GetType().Name] = provider;
        return this;
    }
}