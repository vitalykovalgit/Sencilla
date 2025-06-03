namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ message consumer implementation
/// </summary>
public class RabbitMQConsumer : IRabbitMQConsumer, IDisposable
{
    readonly IRabbitMQConnectionFactory _connectionFactory;
    readonly IRabbitMQTopologyManager _topologyManager;
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<RabbitMQConsumer> _logger;
    readonly RabbitMQOptions _options;
    
    IModel? _channel;
    EventingBasicConsumer? _consumer;
    string? _consumerTag;
    readonly CancellationTokenSource _cancellationTokenSource = new();

    public RabbitMQConsumer(
        IRabbitMQConnectionFactory connectionFactory,
        IRabbitMQTopologyManager topologyManager,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _topologyManager = topologyManager;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartConsumingAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (_options.AutoDeclareTopology)
        {
            var arguments = new Dictionary<string, object>();
            
            if (_options.EnableDeadLetterQueue)
            {
                arguments["x-dead-letter-exchange"] = _options.DeadLetterExchange;
                arguments["x-dead-letter-routing-key"] = queueName;
            }

            await _topologyManager.DeclareQueueAsync(queueName, arguments: arguments);
            
            if (_options.EnableDeadLetterQueue)
            {
                await _topologyManager.SetupDeadLetterQueueAsync(queueName);
            }
        }

        _channel = _connectionFactory.CreateChannel();
        _consumer = new EventingBasicConsumer(_channel);
        
        _consumer.Received += async (sender, args) =>
        {
            await ProcessMessageAsync(args);
        };

        _consumerTag = _channel.BasicConsume(queueName, false, _consumer);
        _logger.LogInformation("Started consuming from queue {QueueName}", queueName);

        // Keep consuming until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer cancellation requested");
        }
    }

    public Task StopConsumingAsync()
    {
        _cancellationTokenSource.Cancel();
        
        if (_channel != null && !string.IsNullOrEmpty(_consumerTag))
        {
            _channel.BasicCancel(_consumerTag);
            _logger.LogInformation("Stopped consuming messages");
        }

        return Task.CompletedTask;
    }

    async Task ProcessMessageAsync(BasicDeliverEventArgs args)
    {
        var messageId = args.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
        var correlationId = args.BasicProperties.CorrelationId;
        var messageType = args.BasicProperties.Type;

        try
        {
            _logger.LogDebug("Processing message {MessageId} of type {MessageType}", messageId, messageType);

            var body = args.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            if (string.IsNullOrEmpty(messageType))
            {
                _logger.LogWarning("Message {MessageId} has no type information", messageId);
                _channel?.BasicNack(args.DeliveryTag, false, false);
                return;
            }

            var type = Type.GetType(messageType);
            if (type == null)
            {
                _logger.LogWarning("Cannot resolve type {MessageType} for message {MessageId}", messageType, messageId);
                _channel?.BasicNack(args.DeliveryTag, false, false);
                return;
            }            // Deserialize the message wrapper
            var messageWrapperType = typeof(Message<>).MakeGenericType(type);
            var messageWrapper = JsonSerializer.Deserialize(json, messageWrapperType);

            if (messageWrapper == null)
            {
                _logger.LogWarning("Failed to deserialize message {MessageId}", messageId);
                _channel?.BasicNack(args.DeliveryTag, false, false);
                return;
            }

            // Get the payload from the wrapper
            var payloadProperty = messageWrapperType.GetProperty("Payload");
            var payload = payloadProperty?.GetValue(messageWrapper);

            if (payload == null)
            {
                _logger.LogWarning("Message {MessageId} has null payload", messageId);
                _channel?.BasicNack(args.DeliveryTag, false, false);
                return;
            }

            // Find and execute handlers
            await ExecuteHandlersAsync(payload, type);

            _channel?.BasicAck(args.DeliveryTag, false);
            _logger.LogDebug("Successfully processed message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", messageId);
            
            // Check retry count and handle accordingly
            var retryCount = GetRetryCount(args.BasicProperties);
            if (retryCount < _options.MaxRetryAttempts)
            {
                await RetryMessageAsync(args, retryCount + 1);
            }
            else
            {
                _logger.LogError("Maximum retry attempts reached for message {MessageId}. Sending to dead letter queue", messageId);
                _channel?.BasicNack(args.DeliveryTag, false, false);
            }
        }
    }

    async Task ExecuteHandlersAsync(object payload, Type payloadType)
    {
        var handlerType = typeof(IMessageHandler<>).MakeGenericType(payloadType);
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            try
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, [payload]);
                    if (task != null)
                    {
                        await task;
                    }
                }
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing handler {HandlerType} for message type {MessageType}", 
                    handler?.GetType().Name ?? "Unknown", payloadType.Name);
                throw;
            }
        }
    }

    async Task RetryMessageAsync(BasicDeliverEventArgs args, int retryCount)
    {
        _logger.LogWarning("Retrying message {MessageId}, attempt {RetryCount}/{MaxRetries}", 
            args.BasicProperties.MessageId, retryCount, _options.MaxRetryAttempts);

        // Add delay before retry
        await Task.Delay(_options.RetryDelayMilliseconds);

        // Update retry count in message properties
        var newProperties = _channel!.CreateBasicProperties();
        newProperties.MessageId = args.BasicProperties.MessageId;
        newProperties.CorrelationId = args.BasicProperties.CorrelationId;
        newProperties.Type = args.BasicProperties.Type;
        newProperties.Persistent = true;
        newProperties.Headers = new Dictionary<string, object>(args.BasicProperties.Headers ?? new Dictionary<string, object>())
        {
            ["x-retry-count"] = retryCount
        };

        // Republish the message
        _channel.BasicPublish("", args.RoutingKey, newProperties, args.Body);
        _channel.BasicAck(args.DeliveryTag, false);
    }

    int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers?.TryGetValue("x-retry-count", out var retryCountObj) == true)
        {
            return Convert.ToInt32(retryCountObj);
        }
        return 0;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _channel?.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
