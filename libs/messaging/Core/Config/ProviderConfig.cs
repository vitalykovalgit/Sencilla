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
    /// When true (default), stream consumers auto-start as a BackgroundService.
    /// When false, consumers are not auto-started; call ExecuteConsumersAsync manually from your own hosted service.
    /// </summary>
    public bool AutoStartConsumers { get; set; } = true;

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
    public ProviderConfig AddRoutes(Action<RoutesConfig> config)
    {
        config(Routes);
        return this;
    }

    /// <summary>
    /// Fluent alias for <see cref="AddRoutes"/>. Configures message routing.
    /// </summary>
    /// <example>
    /// <code>
    /// c.Route(r => {
    ///     r.Message&lt;MyMessage&gt;().To("queue1");
    ///     r.Messages(typeof(Msg1), typeof(Msg2)).To("queue1");
    /// });
    /// </code>
    /// </example>
    public ProviderConfig Route(Action<RoutesConfig> config) => AddRoutes(config);

    /// <summary>
    /// Creates consumers for multiple queues/topics at once.
    /// Topic subscriptions use the "topic:subscription" format.
    /// </summary>
    /// <example>
    /// <code>
    /// q.AddConsumerFor(["queue1", "queue2", "topic:subscription"], con => {
    ///     con.LoadHandlersFromAssembly();
    ///     con.Process&lt;MyProcessor&gt;();
    /// });
    /// </code>
    /// </example>
    public ProviderConfig AddConsumerFor(string[] streams, Action<ConsumerConfig>? config = null)
    {
        foreach (var stream in streams)
        {
            var parts = stream.Split(':', 2);
            if (parts.Length == 2)
                Consumers.ForTopic(parts[0], parts[1], config);
            else
                Consumers.ForQueue(stream, config);
        }
        return this;
    }

    /// <summary>
    /// Creates a consumer for a single queue.
    /// </summary>
    public ProviderConfig AddConsumerFor(string queue, Action<ConsumerConfig>? config = null)
    {
        Consumers.ForQueue(queue, config);
        return this;
    }

    /// <summary>
    /// Creates a consumer for a queue restricted to handling messages of type <typeparamref name="T"/>.
    /// </summary>
    public ProviderConfig AddConsumerFor<T>(string queue)
    {
        Consumers.ForQueue(queue, c => c.HandleOnly<T>());
        return this;
    }

    /// <summary>
    /// Creates a consumer for a queue restricted to handling the specified message types.
    /// </summary>
    public ProviderConfig AddConsumerFor(string queue, Type[] types)
    {
        Consumers.ForQueue(queue, c => c.HandleOnly(types));
        return this;
    }

    /// <summary>
    /// Creates a consumer for a queue restricted to handling messages matching the specified namespace strings.
    /// </summary>
    public ProviderConfig AddConsumerFor(string queue, string[] namespaces)
    {
        Consumers.ForQueue(queue, c => c.HandleOnly(namespaces));
        return this;
    }

    /// <summary>
    /// Defines a queue with optional stream-level configuration.
    /// </summary>
    public ProviderConfig DefineQueue(string name, Action<StreamConfig>? config = null)
    {
        Streams.AddQueue(name, config);
        return this;
    }

    /// <summary>
    /// Fluent alias for <see cref="DefineQueue"/>.
    /// </summary>
    public ProviderConfig ForQueue(string name, Action<StreamConfig>? config = null) => DefineQueue(name, config);

    /// <summary>
    /// Defines multiple queues with shared stream-level configuration.
    /// </summary>
    public ProviderConfig ForQueues(string[] names, Action<StreamConfig>? config = null)
    {
        Streams.AddQueues(names, config);
        return this;
    }

    /// <summary>
    /// Defines a topic with optional stream-level configuration.
    /// </summary>
    public ProviderConfig DefineTopic(string name, Action<StreamConfig>? config = null)
    {
        Streams.AddTopic(name, config);
        return this;
    }
}