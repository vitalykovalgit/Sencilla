namespace Sencilla.Messaging;

[DisableInjection]
public class StreamsConfig(ProviderConfig providerConfig)
{
    /// <summary>
    /// The default stream configuration.
    /// </summary>
    /// <returns></returns>
    private readonly StreamConfig DefaultStreamConfig = new(providerConfig);

    /// <summary>
    /// A collection of all stream configurations.
    /// </summary>
    /// <returns></returns>
    private readonly ConcurrentDictionary<string, StreamConfig> Streams = new();

    public StreamConfig? GetConfig(string name)
    {
        //TODO: Merge default config with specific stream config
        Streams.TryGetValue(name, out var config);
        return config;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public StreamsConfig WithOptions(Action<StreamConfig> config)
    {
        config(DefaultStreamConfig);
        return this;
    }

    public StreamsConfig AddStream(string name, Action<StreamConfig>? config = null)
    {
        var streamConfig = Streams.GetOrAdd(name, _ => new StreamConfig(providerConfig, DefaultStreamConfig)
        {
            Name = name
        });
        config?.Invoke(streamConfig);
        return this;
    }

    public StreamsConfig AddStreams(IEnumerable<string> names, Action<StreamConfig>? config = null)
    {
        foreach (var name in names)
            AddStream(name, config);
        return this;
    }

    public StreamsConfig AddQueue(string name, Action<StreamConfig>? config = null)
    {
        return AddStream(name, config);
    }

    public StreamsConfig AddQueues(IEnumerable<string> names, Action<StreamConfig>? config = null)
    {
        foreach (var name in names)
            AddQueue(name, config);
        return this;
    }

    public StreamsConfig AddTopic(string name, Action<StreamConfig>? config = null)
    {
        return AddStream(name, options =>
        {
            config?.Invoke(options);
            options.Topic = true; // Mark as topic
        });
    }

    public StreamsConfig AddTopics(IEnumerable<string> names, Action<StreamConfig>? config = null)
    {
        foreach (var name in names)
            AddTopic(name, config);
        return this;
    }
}

