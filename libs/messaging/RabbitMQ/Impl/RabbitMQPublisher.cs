namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ message publisher implementation
/// </summary>
public class RabbitMQPublisher(
    IRabbitMQConnectionFactory connectionFactory,
    IRabbitMQTopologyManager topologyManager,
    IOptions<RabbitMQOptions> options,
    ILogger<RabbitMQPublisher> logger) : IRabbitMQPublisher
{
    readonly RabbitMQOptions Options = options.Value;

    public async Task PublishAsync<T>(T message, string? exchange = null, string? routingKey = null) where T : class
    {
        var exchangeName = exchange ?? Options.DefaultExchange;
        var routing = routingKey ?? typeof(T).Name.ToLowerInvariant();

        if (Options.AutoDeclareTopology && !string.IsNullOrEmpty(exchangeName))
        {
            await topologyManager.DeclareExchangeAsync(exchangeName);
        }

        var messageWrapper = CreateMessageWrapper(message);
        var body = SerializeMessage(messageWrapper);

        using var channel = connectionFactory.CreateChannel();
        
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = messageWrapper.Id.ToString();
        properties.CorrelationId = messageWrapper.CorrelationId.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";
        properties.ContentEncoding = "utf-8";
        properties.Type = typeof(T).FullName;

        if (Options.MessageTtlMilliseconds > 0)
        {
            properties.Expiration = Options.MessageTtlMilliseconds.ToString();
        }

        try
        {
            channel.BasicPublish(exchangeName, routing, properties, body);
            logger.LogDebug("Published message {MessageId} to exchange {Exchange} with routing key {RoutingKey}", 
                messageWrapper.Id, exchangeName, routing);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish message {MessageId}", messageWrapper.Id);
            throw;
        }
    }

    public async Task PublishToQueueAsync<T>(T message, string queueName) where T : class
    {
        if (Options.AutoDeclareTopology)
        {
            var arguments = new Dictionary<string, object>();
            
            if (Options.EnableDeadLetterQueue)
            {
                arguments["x-dead-letter-exchange"] = Options.DeadLetterExchange;
                arguments["x-dead-letter-routing-key"] = queueName;
            }

            await topologyManager.DeclareQueueAsync(queueName, arguments: arguments);
            
            if (Options.EnableDeadLetterQueue)
            {
                await topologyManager.SetupDeadLetterQueueAsync(queueName);
            }
        }

        await PublishAsync(message, "", queueName);
    }

    Message<T> CreateMessageWrapper<T>(T payload) where T : class
    {
        return new Message<T>
        {
            Payload = payload,
            Type = MessageType.Event,
            Name = typeof(T).Name,
            Namespace = typeof(T).FullName
        };
    }    byte[] SerializeMessage<T>(Message<T> message) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }
}
