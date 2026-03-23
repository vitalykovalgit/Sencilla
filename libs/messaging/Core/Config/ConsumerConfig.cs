namespace Sencilla.Messaging;

/// <summary>
/// Configuration for an individual message consumer, including retry policies,
/// concurrency settings, message filtering, and processing strategies.
/// </summary>
public class ConsumerConfig
{
    public ConsumerConfig(ConsumerConfig? config = null)
    {
        if (config != null)
        {
            AutoAck = config.AutoAck;
            Exclusive = config.Exclusive;

            StreamName = config.StreamName;
            StreamSubscription = config.StreamSubscription;

            PrefetchCount = config.PrefetchCount;
            PoolingIntervalInMs = config.PoolingIntervalInMs;
            MaxConcurrentHandlers = config.MaxConcurrentHandlers;
            MaxRetries = config.MaxRetries;
            MaxConsumerPerQueue = config.MaxConsumerPerQueue;
            LoadHandlersFromAssemblies = config.LoadHandlersFromAssemblies;
            CustomProcessorType = config.CustomProcessorType;

            if (config.Processing is not null)
                Processing = config.Processing;

            if (config.AllowedTypes.Count > 0)
                AllowedTypes = [.. config.AllowedTypes];

            if (config.AllowedNamespaces.Count > 0)
                AllowedNamespaces = [.. config.AllowedNamespaces];
        }
    }

    /// <summary>
    /// Name of the stream this consumer reads from.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// Subscription name for topic-based streams.
    /// </summary>
    public string? StreamSubscription { get; set; }

    /// <summary>
    /// The maximum number of concurrent handlers for this consumer.
    /// Controls how many messages can be processed in parallel.
    /// </summary>
    public int MaxConcurrentHandlers { get; set; } = 1;

    /// <summary>
    /// Indicates whether the consumer should use asynchronous message handlers.
    /// </summary>
    public bool AsyncHandlers { get; set; } = false;

    /// <summary>
    /// The number of messages to prefetch into the consumer's buffer.
    /// </summary>
    public int PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Indicates whether the consumer should automatically acknowledge messages after processing.
    /// </summary>
    public bool AutoAck { get; set; } = true;

    /// <summary>
    /// The interval in milliseconds at which the consumer polls for new messages.
    /// </summary>
    public int PoolingIntervalInMs { get; set; } = 1000;

    /// <summary>
    /// Indicates whether the consumer should be exclusive to this queue.
    /// </summary>
    public bool Exclusive { get; set; } = false;

    /// <summary>
    /// Maximum number of retry attempts for failed message processing.
    /// </summary>
    public int MaxRetries { get; set; } = 0;

    /// <summary>
    /// Number of consumer instances per queue. Each instance processes messages independently.
    /// </summary>
    public int MaxConsumerPerQueue { get; set; } = 1;

    /// <summary>
    /// When true, message handlers are discovered from registered assemblies via DI scanning.
    /// </summary>
    public bool LoadHandlersFromAssemblies { get; set; } = true;

    /// <summary>
    /// Custom processor type that replaces the default message deserialization and handler dispatch.
    /// Must implement <see cref="IMessageHandler{String}"/> or similar raw-message processing contract.
    /// </summary>
    public Type? CustomProcessorType { get; private set; }

    /// <summary>
    /// Processing strategy configuration (ByNamespace, ByType).
    /// </summary>
    public ProcessingConfig? Processing { get; private set; }

    /// <summary>
    /// Set of types that this consumer is restricted to handling.
    /// When empty, all message types are handled.
    /// </summary>
    public HashSet<Type> AllowedTypes { get; private set; } = [];

    /// <summary>
    /// Set of namespace strings that this consumer is restricted to handling.
    /// When empty, all namespaces are handled.
    /// </summary>
    public HashSet<string> AllowedNamespaces { get; private set; } = [];

    /// <summary>
    /// Applies common options to this consumer configuration.
    /// </summary>
    public ConsumerConfig WithOptions(Action<ConsumerConfig> config)
    {
        config(this);
        return this;
    }

    /// <summary>
    /// Restricts this consumer to only handle messages of type <typeparamref name="T"/>.
    /// </summary>
    public ConsumerConfig HandleOnly<T>()
    {
        AllowedTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Restricts this consumer to only handle messages matching the specified namespace strings.
    /// These are matched against the message's Namespace field.
    /// </summary>
    public ConsumerConfig HandleOnly(params string[] namespaces)
    {
        foreach (var ns in namespaces)
            AllowedNamespaces.Add(ns);
        return this;
    }

    /// <summary>
    /// Restricts this consumer to only handle messages of the specified types.
    /// </summary>
    public ConsumerConfig HandleOnly(params Type[] types)
    {
        foreach (var type in types)
            AllowedTypes.Add(type);
        return this;
    }

    /// <summary>
    /// Sets a custom processor that receives raw message data and handles parsing/dispatching.
    /// Overrides the default namespace/type-based resolution.
    /// </summary>
    public ConsumerConfig Process<TProcessor>() where TProcessor : class
    {
        CustomProcessorType = typeof(TProcessor);
        return this;
    }

    /// <summary>
    /// Configures the default message processing strategy (ByNamespace, ByType).
    /// </summary>
    public ConsumerConfig Process(Action<ProcessingConfig> config)
    {
        Processing ??= new ProcessingConfig();
        config(Processing);
        return this;
    }

    /// <summary>
    /// Enables automatic handler discovery from registered assemblies.
    /// This is the default behavior.
    /// </summary>
    public ConsumerConfig LoadHandlersFromAssembly()
    {
        LoadHandlersFromAssemblies = true;
        return this;
    }

    /// <summary>
    /// Returns true if this consumer has any message filtering configured (type or namespace restrictions).
    /// </summary>
    public bool HasFilters => AllowedTypes.Count > 0 || AllowedNamespaces.Count > 0;

    /// <summary>
    /// Checks whether a given message type is allowed by this consumer's filters.
    /// Returns true if no filters are configured or if the type passes the filter.
    /// </summary>
    public bool IsTypeAllowed(Type type)
    {
        if (!HasFilters) return true;
        if (AllowedTypes.Contains(type)) return true;

        var fullName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        return AllowedNamespaces.Any(ns => fullName.Contains(ns, StringComparison.OrdinalIgnoreCase));
    }
}
