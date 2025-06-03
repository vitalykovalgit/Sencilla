# Sencilla.Messaging.RabbitMQ

RabbitMQ implementation for the Sencilla messaging framework, providing reliable message publishing, consuming, and routing capabilities.

## Features

- **Message Publishing**: Publish messages to queues or exchanges with routing keys
- **Message Consuming**: Consume messages with automatic retry and dead letter queue support
- **Topology Management**: Automatic declaration of exchanges, queues, and bindings
- **Health Checks**: Built-in health checks for monitoring RabbitMQ connectivity
- **Middleware Integration**: Seamless integration with Sencilla messaging middleware pipeline
- **Configuration**: Flexible configuration options for various RabbitMQ scenarios
- **Error Handling**: Comprehensive error handling with retry mechanisms
- **Dead Letter Queues**: Automatic setup of dead letter queues for failed messages

## Installation

Add the RabbitMQ messaging package to your project:

```xml
<PackageReference Include="Sencilla.Messaging.RabbitMQ" Version="1.0.0" />
```

## Configuration

### Basic Configuration

```csharp
services.AddRabbitMQMessaging(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672/";
    options.QueuePrefix = "myapp";
    options.MaxRetryAttempts = 3;
    options.EnableDeadLetterQueue = true;
});
```

### Configuration from appsettings.json

```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "DefaultExchange": "",
    "QueuePrefix": "sencilla",
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 5000,
    "EnableDeadLetterQueue": true,
    "DeadLetterExchange": "dlx",
    "DeadLetterQueue": "dlq",
    "MessageTtlMilliseconds": 0,
    "AutoDeclareTopology": true,
    "PrefetchCount": 10
  }
}
```

```csharp
services.AddRabbitMQMessaging(configuration, "RabbitMQ");
```

## Usage

### 1. Define Message Classes

```csharp
// Simple message
public class UserRegisteredEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public DateTime RegisteredAt { get; set; }
}

// Message with queue routing
[Queue("user-notifications")]
public class NotificationMessage
{
    public string UserId { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
}

// Message with topic routing
[Topic("events", "user.registered")]
public class UserEventMessage
{
    public string UserId { get; set; }
    public string EventType { get; set; }
    public object Data { get; set; }
}
```

### 2. Publishing Messages

```csharp
public class UserService
{
    private readonly IMessageDispatcher _messageDispatcher;

    public UserService(IMessageDispatcher messageDispatcher)
    {
        _messageDispatcher = messageDispatcher;
    }

    public async Task RegisterUserAsync(string email)
    {
        // Register user logic...
        
        // Publish event - will use default routing (queue name = message type)
        await _messageDispatcher.Publish(new UserRegisteredEvent
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            RegisteredAt = DateTime.UtcNow
        });

        // Publish to specific queue
        await _messageDispatcher.Publish(new NotificationMessage
        {
            UserId = userId,
            Message = "Welcome to our service!",
            Type = "Welcome"
        });
    }
}
```

### 3. Direct RabbitMQ Publishing

```csharp
public class OrderService
{
    private readonly IRabbitMQPublisher _publisher;

    public OrderService(IRabbitMQPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Publish to specific queue
        await _publisher.PublishToQueueAsync(order, "order-processing");

        // Publish to exchange with routing key
        await _publisher.PublishAsync(order, "orders", $"order.{order.Status}");
    }
}
```

### 4. Message Handlers

```csharp
public class UserRegisteredEventHandler : IMessageHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredEventHandler> _logger;
    private readonly IEmailService _emailService;

    public UserRegisteredEventHandler(
        ILogger<UserRegisteredEventHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(UserRegisteredEvent @event)
    {
        _logger.LogInformation("Processing user registration for {UserId}", @event.UserId);
        
        await _emailService.SendWelcomeEmailAsync(@event.Email);
        
        _logger.LogInformation("Welcome email sent to {Email}", @event.Email);
    }
}

public class NotificationHandler : IMessageHandler<NotificationMessage>
{
    private readonly INotificationService _notificationService;

    public NotificationHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task HandleAsync(NotificationMessage message)
    {
        await _notificationService.SendNotificationAsync(
            message.UserId, 
            message.Message, 
            message.Type);
    }
}
```

### 5. Service Registration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Sencilla core services
        services.AddSencilla(configuration);

        // Add RabbitMQ messaging
        services.AddRabbitMQMessaging(configuration, "RabbitMQ");

        // Register message handlers
        services.AddTransient<IMessageHandler<UserRegisteredEvent>, UserRegisteredEventHandler>();
        services.AddTransient<IMessageHandler<NotificationMessage>, NotificationHandler>();

        // Other services...
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<INotificationService, NotificationService>();
    }
}
```

## Advanced Features

### Custom Queue Configuration

```csharp
public class CustomConsumerService : BackgroundService
{
    private readonly IRabbitMQConsumer _consumer;

    public CustomConsumerService(IRabbitMQConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.StartConsumingAsync("priority-orders", stoppingToken);
    }
}
```

### Topology Management

```csharp
public class RabbitMQSetupService
{
    private readonly IRabbitMQTopologyManager _topologyManager;

    public RabbitMQSetupService(IRabbitMQTopologyManager topologyManager)
    {
        _topologyManager = topologyManager;
    }

    public async Task SetupTopologyAsync()
    {
        // Declare exchanges
        await _topologyManager.DeclareExchangeAsync("events", ExchangeType.Topic);
        await _topologyManager.DeclareExchangeAsync("notifications", ExchangeType.Direct);

        // Declare queues
        await _topologyManager.DeclareQueueAsync("user-events");
        await _topologyManager.DeclareQueueAsync("email-notifications");

        // Bind queues to exchanges
        await _topologyManager.BindQueueAsync("user-events", "events", "user.*");
        await _topologyManager.BindQueueAsync("email-notifications", "notifications", "email");
    }
}
```

### Health Checks

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRabbitMQMessaging(configuration);
        
        // Health checks are automatically registered
        services.AddHealthChecks(); // RabbitMQ health check is included
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHealthChecks("/health");
    }
}
```

## Error Handling and Retry Logic

The RabbitMQ implementation includes built-in retry logic:

1. **Automatic Retries**: Messages that fail processing are automatically retried up to the configured maximum attempts
2. **Exponential Backoff**: Configurable delay between retry attempts
3. **Dead Letter Queues**: Messages that exceed retry limits are moved to dead letter queues for manual inspection
4. **Logging**: Comprehensive logging for monitoring and debugging

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `ConnectionString` | RabbitMQ connection string | `amqp://guest:guest@localhost:5672/` |
| `DefaultExchange` | Default exchange name | `""` (default exchange) |
| `QueuePrefix` | Prefix for queue names | `"sencilla"` |
| `MaxRetryAttempts` | Maximum retry attempts for failed messages | `3` |
| `RetryDelayMilliseconds` | Delay between retries | `5000` |
| `EnableDeadLetterQueue` | Enable dead letter queue support | `true` |
| `DeadLetterExchange` | Dead letter exchange name | `"dlx"` |
| `DeadLetterQueue` | Dead letter queue suffix | `"dlq"` |
| `MessageTtlMilliseconds` | Message time-to-live (0 = no expiration) | `0` |
| `AutoDeclareTopology` | Automatically declare exchanges and queues | `true` |
| `PrefetchCount` | Consumer prefetch count | `10` |

## Best Practices

1. **Use Attributes**: Use `[Queue]` and `[Topic]` attributes to specify routing
2. **Handle Failures**: Implement proper error handling in message handlers
3. **Monitor Health**: Use health checks to monitor RabbitMQ connectivity
4. **Configure Retries**: Set appropriate retry limits based on your use case
5. **Use Dead Letter Queues**: Enable DLQ for failed message analysis
6. **Log Operations**: Implement comprehensive logging for troubleshooting

## Dependencies

- RabbitMQ.Client 6.8.1+
- Microsoft.Extensions.Hosting.Abstractions 9.0.1+
- Microsoft.Extensions.Logging.Abstractions 9.0.1+
- Sencilla.Messaging (Core)
- Sencilla.Core

## License

This project is part of the Sencilla framework.
