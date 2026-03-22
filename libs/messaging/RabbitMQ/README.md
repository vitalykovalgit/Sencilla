# Sencilla.Messaging.RabbitMQ

RabbitMQ provider for the Sencilla messaging framework.

## Features

- Integrates with the Sencilla messaging pipeline (dispatcher, middleware, consumers)
- Supports queues (point-to-point) and topics (pub/sub via fanout exchanges)
- Automatic topology declaration (queues, exchanges, dead letter queues)
- Health checks for monitoring RabbitMQ connectivity
- Fluent configuration API consistent with other Sencilla providers

## Configuration

```csharp
services.AddSencillaMessaging(o =>
{
    o.UseRabbitMQ(c =>
    {
        // RabbitMQ connection options
        c.WithOptions(opts =>
        {
            opts.ConnectionString = "amqp://guest:guest@localhost:5672/";
            opts.PrefetchCount = 10;
            opts.EnableDeadLetterQueue = true;
        });

        // Define streams (queues and topics)
        c.AddStreams(s =>
        {
            s.AddQueue("order-events");
            s.AddQueue("notifications");
            s.AddTopic("broadcast-events");
        });

        // Route message types to streams
        c.AddRoutes(r =>
        {
            r.Send<OrderPlacedEvent>().ToStream("order-events");
            r.Send<NotificationMessage>().ToStream("notifications");
        });

        // Configure consumers
        c.AddConsumers(consumers =>
        {
            consumers.ForQueue("order-events", cfg =>
            {
                cfg.MaxConcurrentHandlers = 5;
                cfg.PrefetchCount = 10;
            });
            consumers.ForQueue("notifications");
        });
    });
});
```

### Attribute-based routing

```csharp
[Stream("order-events")]
public class OrderPlacedEvent
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}
```

## Publishing messages

```csharp
public class OrderService(IMessageDispatcher dispatcher)
{
    public async Task PlaceOrder(Order order)
    {
        // Process order...

        await dispatcher.Send(new OrderPlacedEvent
        {
            OrderId = order.Id,
            Amount = order.Total
        });
    }
}
```

## Handling messages

```csharp
public class OrderPlacedHandler : IMessageHandler<OrderPlacedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent message, CancellationToken token)
    {
        // Process the order event
    }
}

// Register handler
services.AddTransient<IMessageHandler<OrderPlacedEvent>, OrderPlacedHandler>();
```

## Consumer modes

By default, consumers auto-start as a `BackgroundService`. For standalone worker services:

```csharp
c.AutoStartConsumers = false; // Register as singleton, not hosted service
```

Then inject and run manually:

```csharp
public class MyWorker(
    MessageStreamsConsumer<RabbitMQStreamProvider, RabbitMQProviderConfig> consumer) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => consumer.ExecuteConsumersAsync(stoppingToken);
}
```

## Topology management

For advanced scenarios, inject `IRabbitMQTopologyManager` directly:

```csharp
public class SetupService(IRabbitMQTopologyManager topology)
{
    public async Task SetupAsync()
    {
        await topology.DeclareExchangeAsync("events", ExchangeType.Topic);
        await topology.DeclareQueueAsync("user-events");
        await topology.BindQueueAsync("user-events", "events", "user.*");
        await topology.SetupDeadLetterQueueAsync("user-events");
    }
}
```

## Health checks

Health checks are registered automatically with the `rabbitmq` tag:

```csharp
app.MapHealthChecks("/health");
```

## Configuration options

| Option | Description | Default |
|--------|-------------|---------|
| `ConnectionString` | AMQP connection URI | `amqp://guest:guest@localhost:5672/` |
| `DefaultExchange` | Default exchange name | `""` (nameless) |
| `PrefetchCount` | Consumer prefetch count | `10` |
| `AutoDeclareTopology` | Auto-declare queues/exchanges | `true` |
| `EnableDeadLetterQueue` | Enable DLQ support | `true` |
| `DeadLetterExchange` | DLX name | `dlx` |
| `DeadLetterQueue` | DLQ suffix | `dlq` |
| `MessageTtlMilliseconds` | Message TTL (0 = no expiration) | `0` |
