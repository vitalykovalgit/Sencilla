namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQTopologyManager(
    IRabbitMQConnectionFactory connectionFactory,
    RabbitMQProviderOptions options,
    ILogger<RabbitMQTopologyManager> logger) : IRabbitMQTopologyManager
{
    public async Task DeclareExchangeAsync(string exchangeName, string exchangeType = ExchangeType.Direct, bool durable = true, bool autoDelete = false)
    {
        await using var channel = await connectionFactory.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable, autoDelete);
        logger.LogDebug("Declared exchange: {ExchangeName} of type {ExchangeType}", exchangeName, exchangeType);
    }

    public async Task DeclareQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object?>? arguments = null)
    {
        await using var channel = await connectionFactory.CreateChannelAsync();
        var args = arguments ?? new Dictionary<string, object?>();

        if (options.MessageTtlMilliseconds > 0)
            args["x-message-ttl"] = options.MessageTtlMilliseconds;

        await channel.QueueDeclareAsync(queueName, durable, exclusive, autoDelete, args);
        logger.LogDebug("Declared queue: {QueueName}", queueName);
    }

    public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey)
    {
        await using var channel = await connectionFactory.CreateChannelAsync();
        await channel.QueueBindAsync(queueName, exchangeName, routingKey);
        logger.LogDebug("Bound queue {QueueName} to exchange {ExchangeName} with routing key {RoutingKey}",
            queueName, exchangeName, routingKey);
    }

    public async Task SetupDeadLetterQueueAsync(string mainQueueName)
    {
        if (!options.EnableDeadLetterQueue)
            return;

        var dlxName = options.DeadLetterExchange;
        var dlqName = $"{mainQueueName}.{options.DeadLetterQueue}";

        await DeclareExchangeAsync(dlxName, ExchangeType.Direct);
        await DeclareQueueAsync(dlqName);
        await BindQueueAsync(dlqName, dlxName, mainQueueName);

        logger.LogInformation("Setup dead letter queue topology for {MainQueue}", mainQueueName);
    }
}
