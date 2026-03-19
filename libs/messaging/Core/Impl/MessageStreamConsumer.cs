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
    private static readonly MethodInfo BaseExecuteMethod =
        typeof(IMessageHandlerExecutor).GetMethod(nameof(IMessageHandlerExecutor.ExecuteAsync))!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> ExecuteMethodCache = [];

    private readonly SemaphoreSlim Semaphore = new(consumerConfig.MaxConcurrentHandlers);

    public async Task Execute(CancellationToken stoppingToken)
    {
        var consumerTag = tag ?? consumerConfig.StreamName ?? "default";

        logger.LogInformation("Consumer {Tag} started for stream {Stream} at {Time}",
            consumerTag, consumerConfig.StreamName, DateTimeOffset.UtcNow);

        var reader = provider.GetOrCreateStream(streamConfig);
        var inflightTasks = new List<Task>();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var json = await reader.Read(stoppingToken);
                if (json is null) continue;

                await Semaphore.WaitAsync(stoppingToken);

                var task = ProcessWithSemaphoreRelease(json, stoppingToken);
                inflightTasks.Add(task);

                // Periodically clean completed tasks to avoid unbounded list growth
                if (inflightTasks.Count > consumerConfig.MaxConcurrentHandlers * 2)
                    inflightTasks.RemoveAll(t => t.IsCompleted);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Consumer {Tag} stopping for stream {Stream}",
                consumerTag, consumerConfig.StreamName);
        }

        // Graceful shutdown: wait for all in-flight messages to complete
        if (inflightTasks.Count > 0)
        {
            logger.LogInformation("Consumer {Tag} waiting for {Count} in-flight messages on stream {Stream}",
                consumerTag, inflightTasks.Count, consumerConfig.StreamName);
            await Task.WhenAll(inflightTasks);
        }

        logger.LogInformation("Consumer {Tag} stopped for stream {Stream} at {Time}",
            consumerTag, consumerConfig.StreamName, DateTimeOffset.UtcNow);
    }

    private async Task ProcessWithSemaphoreRelease(string json, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessMessageAsync(json, cancellationToken);
        }
        finally
        {
            Semaphore.Release();
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

            var executeMethod = ExecuteMethodCache.GetOrAdd(payloadType, type =>
                BaseExecuteMethod.MakeGenericMethod(type));

            var task = (Task)executeMethod.Invoke(executor, [typedMessage, scope.ServiceProvider, cancellationToken])!;
            await task;

            logger.LogInformation("Processed message {MessageId} of type {Type} from stream {Stream}",
                message.Id, payloadType.Name, consumerConfig.StreamName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Message processing cancelled during shutdown on stream {Stream}", consumerConfig.StreamName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message from stream {Stream}", consumerConfig.StreamName);
        }
    }
}

