namespace Sencilla.Messaging;

public class ProviderConfig
{
    /// <summary>
    /// Configuration for consumers in the messaging system.
    /// </summary>
    public ConsumersConfig Consumers { get; }

    /// <summary>
    /// Configuration for streams in the messaging system.
    /// </summary>
    /// <value></value>
    public StreamsConfig Streams { get; }

    /// <summary>
    /// Configuration for routing messages to specific handlers or endpoints.
    /// </summary>
    public RoutesConfig Routes { get; }

    /// <summary>
    /// 
    /// </summary>
    public ProviderConfig()
    {
        Consumers = new ConsumersConfig();
        Streams = new StreamsConfig(this);
        Routes = new RoutesConfig();
    }

    /// <summary>
    /// Adds consumers to the messaging configuration.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public ProviderConfig AddConsumers(Action<ConsumersConfig> config)
    {
        config(Consumers);
        // Add Host Services
        return this;
    }

    /// <summary>
    /// Adds streams to the messaging configuration.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public ProviderConfig AddStreams(Action<StreamsConfig> config)
    {
        config(Streams);
        return this;
    }

    /// <summary>
    /// Adds routing configurations to the messaging system.
    /// </summary>
    /// <param name="config">The routing configuration action.</param>
    /// <returns>  </returns>
    public ProviderConfig AddRoutes(Action<RoutesConfig> config)
    {
        config(Routes);
        return this;
    }
}