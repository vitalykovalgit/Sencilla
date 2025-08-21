namespace Sencilla.Messaging;

public class StreamConfig
{
    ProviderConfig ProviderConfig { get; }

    public StreamConfig(ProviderConfig providerConfig, StreamConfig? clone = null)
    {
        ProviderConfig = providerConfig;
        if (clone != null)
        {
            Name = clone.Name;
            Topic = clone.Topic;
            Durable = clone.Durable;
            AutoCreate = clone.AutoCreate;
            MaxQueueSize = clone.MaxQueueSize;
        }
    }

    /// <summary>
    /// The name of the stream to be consumed.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Indicates whether the stream should be automatically created if it does not exist.
    /// </summary>
    /// <value></value>
    public bool? AutoCreate { get; set; }

    /// <summary>
    /// True if stream is topic 
    /// </summary>
    /// <value></value>
    public bool? Topic { get; set; }

    /// <summary>
    /// The maximum size of the queue.
    /// </summary>
    /// <value></value>
    public int? MaxQueueSize { get; set; }

    /// <summary>
    /// Indicates whether the queue is durable.
    /// A durable queue will survive broker restarts and retain messages.
    /// </summary>
    public bool Durable { get; set; } = true;    

    public StreamConfig Receive<T>()
    {
        // Implementation for receiving a message from the stream
        ProviderConfig.Routes.Send<T>().ToStream(Name ?? "");
        return this;
    }

    public StreamConfig For<T>()
    {
        return For(typeof(T));
    }

    public StreamConfig For(params Type[] types)
    {
        types.ForEach(type => ProviderConfig.Routes.Send(type).ToStream(Name ?? ""));
        return this;
    }

    public StreamConfig AddConsumer(Action<ConsumerConfig>? config = null)
    {
        //if (Name is not null)
        //    providerConfig.Consumers.AddConsumer(Name ?? "", config);
        return this;
    }
}