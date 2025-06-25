namespace Sencilla.Messaging.InMemoryQueue;

public class QueueConsumerService(
    ILogger<ConsumerService> logger,
    IMessageStreamReader reader,
    IMessageDispatcher dispatcher,
    QueueConfig config)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected async Task Execute(CancellationToken stoppingToken)
    {
        logger.LogInformation("ConsumerService::ExecuteAsync is called at utctime: {time}", DateTimeOffset.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            // read data from the in-memory queue
            var @event = await reader.Dequeue<string>(stoppingToken);

            //System.Text.Json.
            var message = JsonSerializer.Deserialize<Message>(@event);

            // Deserialize Message<T> where T type is determined from Namespace field
            if (message == null || string.IsNullOrEmpty(message.Namespace))
            {
                logger.LogWarning("Received an empty or invalid message.");
                continue;
            }

            var messageType = Type.GetType(message.Namespace);
            if (messageType != null)
            {
                var genericMessageType = typeof(Message<>).MakeGenericType(messageType);
                var deserializedMessage = JsonSerializer.Deserialize(@event, genericMessageType);
                if (deserializedMessage != null)
                {
                    var publishMethod = typeof(IMessageDispatcher).GetMethod(nameof(IMessageDispatcher.Publish))?.MakeGenericMethod(messageType);
                    if (publishMethod != null)
                    {
                        var result = publishMethod.Invoke(dispatcher, [deserializedMessage]);
                        if (result is Task task)
                            await task;
                    }
                    
                    //await dispatcher.Publish(deserializedMessage);
                    logger.LogInformation("Dispatched message of type {MessageType} at {Time}", messageType.Name, DateTimeOffset.UtcNow);
                }
            }
        }
    }
}

