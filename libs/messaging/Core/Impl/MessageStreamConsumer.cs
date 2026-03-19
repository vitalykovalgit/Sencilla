namespace Sencilla.Messaging;

public class MessageStreamConsumer(
    ILogger<MessageStreamConsumer> logger,
    IServiceScopeFactory scopeFactory,
    IMessageHandlerExecutor executor,
    IMessageStreamProvider provider,
    ConsumerConfig consumerConfig,
    StreamConfig streamConfig,
    string? tag = null)
{
    private readonly SemaphoreSlim Semaphore = new(consumerConfig.MaxConcurrentHandlers);

    public async Task Execute(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consumer {Tag} started for stream {Stream} at {Time}",
            tag ?? "default", consumerConfig.StreamName, DateTimeOffset.UtcNow);

        var reader = provider.GetOrCreateStream(streamConfig);

        while (!stoppingToken.IsCancellationRequested)
        {
            var json = await reader.Read(stoppingToken);
            if (json is null) continue;

            await Semaphore.WaitAsync(stoppingToken);

            _ = ProcessMessageAsync(json, stoppingToken).ContinueWith(_ => Semaphore.Release(), TaskScheduler.Default);
        }
    }

    private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<Message>(json);
            if (message is null || string.IsNullOrEmpty(message.Namespace))
            {
                logger.LogWarning("Received message with empty or missing Namespace, skipping");
                return;
            }

            var payloadType = Type.GetType(message.Namespace);
            if (payloadType is null)
            {
                logger.LogWarning("Unknown type {Namespace}, skipping message {MessageId}", message.Namespace, message.Id);
                return;
            }

            var genericMessageType = typeof(Message<>).MakeGenericType(payloadType);
            var typedMessage = JsonSerializer.Deserialize(json, genericMessageType);
            if (typedMessage is null) return;

            using var scope = scopeFactory.CreateScope();

            var executeMethod = typeof(IMessageHandlerExecutor)
                .GetMethod(nameof(IMessageHandlerExecutor.ExecuteAsync))!
                .MakeGenericMethod(payloadType);

            var task = (Task)executeMethod.Invoke(executor, [typedMessage, scope.ServiceProvider, cancellationToken])!;
            await task;

            logger.LogInformation("Processed message {MessageId} of type {Type} from stream {Stream}",
                message.Id, payloadType.Name, consumerConfig.StreamName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message from stream {Stream}", consumerConfig.StreamName);
        }
    }
}

