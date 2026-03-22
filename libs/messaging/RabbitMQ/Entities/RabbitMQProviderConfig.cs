namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQProviderConfig : ProviderConfig
{
    public RabbitMQProviderOptions Options { get; } = new();

    public RabbitMQProviderConfig WithOptions(Action<RabbitMQProviderOptions> config)
    {
        config(Options);
        return this;
    }
}
