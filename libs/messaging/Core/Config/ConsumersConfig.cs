namespace Sencilla.Messaging;

[DisableInjection]
public class ConsumersConfig
{
    private ConsumerConfig DefaultConsumerConfig = new ConsumerConfig();
    private readonly ConcurrentDictionary<string, ConsumerConfig> Consumers = new();

    public bool HasAnyConsumers => Consumers.Count > 0;

    public IEnumerable<ConsumerConfig> GetConsumers()
    {
        return Consumers.Values;
    }

    public ConsumersConfig WithOptions(Action<ConsumerConfig> config)
    {
        config(DefaultConsumerConfig);
        return this;
    }

    public ConsumersConfig ForAllStreams(Action<ConsumerConfig>? config = null)
    {
        //
        return this;
    }

    public ConsumersConfig ForStream(string name, Action<ConsumerConfig>? config = null)
    {
        var consumer = Consumers.GetOrAdd(name, _ => new ConsumerConfig(DefaultConsumerConfig));
        config?.Invoke(consumer);
        consumer.StreamName = name;
        return this;
    }

    public ConsumersConfig ForQueue(string name, Action<ConsumerConfig>? config = null)
    {
        return ForStream(name, o =>
        {
            config?.Invoke(o);
            o.StreamName = name;
        });
    }

    public ConsumersConfig ForTopic(string name, string subscription, Action<ConsumerConfig>? config = null)
    {
        return ForStream(name, o =>
        {
            config?.Invoke(o);
            o.StreamName = name;
            o.StreamSubscription = subscription;
        });
    }

}
