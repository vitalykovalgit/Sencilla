# Messaging

[Home](../../README.md) / [Docs](../index.md) / Messaging

**NuGet:** `Sencilla.Messaging` (core) + provider package
**Namespace:** `Sencilla.Messaging`
**Source:** `libs/messaging/`

> **Status:** Documentation in progress. Core interface reference is complete. Provider-specific guides are planned.

Sencilla Messaging provides a **provider-agnostic message bus**. You write your message handlers once; the provider (RabbitMQ, Kafka, Azure Service Bus, etc.) is chosen at registration time with no code changes.

---

## Architecture

```text
Your Code
    │
    │  dispatcher.Send(message)
    ▼
IMessageDispatcher
    │  routes through middleware pipeline
    ▼
IMessageMiddleware (chain)
    │
    ▼
IMessageStreamProvider   ←── configured provider
    │
    ▼
External Broker (RabbitMQ / Kafka / ServiceBus / Redis / SignalR / InMemory)
    │
    ▼
Consumer → IMessageHandler<TMessage>
```

---

## Core Interfaces

### `IMessageDispatcher`

```csharp
public interface IMessageDispatcher
{
    Task Send<T>(T message, CancellationToken token = default);
    Task Send<T>(Message<T> message, CancellationToken token = default);
}
```

### `IMessageHandler<TMessage>`

```csharp
public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken token);
}
```

### `Message<T>` — Message Envelope

```csharp
public class Message<T>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public T Payload { get; init; }
    public MessageState State { get; set; }   // Pending, Processing, Completed, Failed
    public MessageType Type { get; set; }     // Command, Event, Query
    public Dictionary<string, string> Headers { get; init; } = new();
}
```

---

## Available Providers

| Package | Broker | Notes |
| ------- | ------ | ----- |
| `Sencilla.Messaging.InMemoryQueue` | In-process | Dev, testing, single-instance |
| `Sencilla.Messaging.RabbitMQ` | RabbitMQ | Recommended for microservices |
| `Sencilla.Messaging.Kafka` | Apache Kafka | High-throughput event streaming |
| `Sencilla.Messaging.ServiceBus` | Azure Service Bus | Cloud-native, managed |
| `Sencilla.Messaging.Redis` | Redis | Lightweight pub/sub |
| `Sencilla.Messaging.SignalR` | ASP.NET SignalR | Real-time client push |
| `Sencilla.Messaging.Mediator` | MediatR in-process | Same-process mediator pattern |
| `Sencilla.Messaging.EntityFramework` | EF Core (outbox) | Transactional outbox pattern |
| `Sencilla.Messaging.Scheduler` | Scheduler | Time-based message dispatch |

---

## Quick Start

### 1. Install core + provider

```bash
dotnet add package Sencilla.Messaging
dotnet add package Sencilla.Messaging.InMemoryQueue   # or RabbitMQ, Kafka, etc.
```

### 2. Define a message

```csharp
public record OrderPlacedMessage(Guid OrderId, decimal Total, string CustomerEmail);
```

### 3. Define a handler

```csharp
[Implement]
public class OrderPlacedHandler : IMessageHandler<OrderPlacedMessage>
{
    private readonly IEmailService _email;

    public OrderPlacedHandler(IEmailService email) => _email = email;

    public Task HandleAsync(OrderPlacedMessage message, CancellationToken token)
        => _email.SendOrderConfirmationAsync(message.CustomerEmail, message.OrderId, token);
}
```

### 4. Register messaging

```csharp
// Program.cs
builder.Services.AddSencillaMessaging(options =>
{
    options.UseInMemoryQueue(null);  // swap to UseRabbitMQ(), UseKafka(), etc.
});
```

### 5. Send a message

```csharp
public class OrderService(IMessageDispatcher dispatcher)
{
    public async Task PlaceOrderAsync(Order order)
    {
        // ... business logic ...
        await dispatcher.Send(new OrderPlacedMessage(
            OrderId: order.Id,
            Total: order.Total,
            CustomerEmail: order.CustomerEmail));
    }
}
```

---

## Fluent Configuration API

All configuration is done through the fluent builder inside `AddSencillaMessaging`. Each provider (`UseInMemoryQueue`, `UseRabbitMQ`, etc.) receives a `ProviderConfig` that exposes routing, consumer, queue/topic, and stream definitions.

### Routing

Route message types to queues or topics:

```csharp
options.UseInMemoryQueue(c =>
{
    // Single type to a queue
    c.Route(r => r.Message<OrderPlacedMessage>().To("orders-queue"));

    // Multiple types to the same queue
    c.Route(r => r.Messages(typeof(OrderPlaced), typeof(OrderCancelled)).To("orders-queue"));

    // A single type to multiple queues
    c.Route(r => r.Message<AuditEvent>().To("audit-queue", "analytics-queue"));
});
```

### Consumers

Define consumers for queues and topics:

```csharp
options.UseRabbitMQ(c =>
{
    // Basic consumer
    c.AddConsumerFor("orders-queue");

    // Consumer with configuration
    c.AddConsumerFor("orders-queue", con =>
    {
        con.MaxConcurrentHandlers = 5;
        con.PrefetchCount = 10;
        con.AutoAck = false;
    });

    // Consumer restricted to a single type
    c.AddConsumerFor<OrderPlacedMessage>("orders-queue");

    // Consumer restricted to specific types
    c.AddConsumerFor("orders-queue", new[] { typeof(OrderPlaced), typeof(OrderCancelled) });

    // Consumer restricted by namespace
    c.AddConsumerFor("orders-queue", new[] { "MyApp.Orders", "MyApp.Payments" });

    // Multiple queues + topic subscriptions
    c.AddConsumerFor(new[] { "queue1", "queue2", "events-topic:my-subscription" });
});
```

Topic subscriptions use the `topic:subscription` format in the string array.

### Message Filtering

Use `HandleOnly` on `ConsumerConfig` to restrict which messages a consumer processes:

```csharp
c.AddConsumerFor("orders-queue", con =>
{
    // By type
    con.HandleOnly<OrderPlacedMessage>();
    con.HandleOnly(typeof(OrderCancelled), typeof(OrderRefunded));

    // By namespace (case-insensitive match against AssemblyQualifiedName)
    con.HandleOnly("MyApp.Orders", "MyApp.Payments");
});
```

`IsTypeAllowed(Type)` returns `true` when no filters are configured, or when the type matches a registered type or namespace filter.

### Processing Strategies

Configure how messages are resolved to handlers:

```csharp
c.AddConsumerFor("orders-queue", con =>
{
    // Custom processor — receives raw data and handles parsing/dispatching
    con.Process<MyQueueProcessor>();

    // Default processor with strategy configuration
    con.Process(p =>
    {
        p.ByNamespace(); // resolve via Type.GetType from message namespace (default)
        p.ByType();      // resolve via registered type mapping
    });
});
```

### Queue and Topic Definitions

Define stream-level configuration for queues and topics:

```csharp
options.UseRabbitMQ(c =>
{
    // Define a single queue
    c.DefineQueue("orders-queue", s =>
    {
        s.Durable = true;
        s.AutoCreate = true;
        s.MaxQueueSize = 10000;
    });

    // ForQueue is an alias for DefineQueue
    c.ForQueue("orders-queue");

    // Define multiple queues with shared settings
    c.ForQueues(new[] { "q1", "q2", "q3" }, s => s.AutoCreate = true);

    // Define a topic (sets Topic = true on StreamConfig)
    c.DefineTopic("events-topic");
});
```

### Middleware

Register global middleware on `MessagingConfig`:

```csharp
options.UseMiddleware<MyCustomMiddleware>();
```

### StreamConfig with Consumer

`StreamConfig` supports attaching a consumer directly:

```csharp
c.ForQueue("orders-queue", q =>
{
    q.For<OrderPlacedMessage>();     // route type to this queue
    q.AddConsumer(con =>             // attach consumer
    {
        con.HandleOnly<OrderPlacedMessage>();
        con.Process(p => p.ByType());
    });
});
```

### Full Example

```csharp
builder.Services.AddSencillaMessaging(options =>
{
    options.UseMiddleware<OpenTelemetryMiddleware>();

    options.UseRabbitMQ(c =>
    {
        c.WithOptions(o =>
        {
            o.ConnectionString = "amqp://user:pass@host:5672/";
            o.PrefetchCount = 20;
            o.EnableDeadLetterQueue = true;
        })
        .Route(r =>
        {
            r.Message<OrderPlaced>().To("orders-queue");
            r.Messages(typeof(OrderPlaced), typeof(OrderCancelled)).To("audit-queue");
        })
        .DefineQueue("orders-queue", s => s.AutoCreate = true)
        .DefineQueue("audit-queue")
        .DefineTopic("events-topic")
        .AddConsumerFor("orders-queue", con =>
        {
            con.HandleOnly<OrderPlaced>();
            con.Process(p => p.ByType());
        })
        .AddConsumerFor(new[] { "events-topic:my-sub" });
    });
});
```

### Consumer Auto-Start

By default, consumers start automatically as a `BackgroundService`. Set `AutoStartConsumers = false` to manage startup manually:

```csharp
c.AutoStartConsumers = false;
```

---

## Class-Based Configuration (IMessagingConfig)

Similar to EF Core's `IEntityTypeConfiguration<T>`, you can define messaging configuration in separate classes:

```csharp
public class OrdersMessagingConfig : IMessagingConfig
{
    public void Configure(MessagingConfig options)
    {
        options.UseRabbitMQ(c =>
        {
            c.Route(r => r.Message<OrderPlaced>().To("orders-queue"));
            c.AddConsumerFor("orders-queue", con => con.HandleOnly<OrderPlaced>());
        });
    }
}
```

Discover and apply configurations from assemblies:

```csharp
// Scan specific assemblies
builder.Services.AddSencillaMessaging(options =>
{
    options.ApplyConfigurationsFromAssemblies(typeof(OrdersMessagingConfig).Assembly);
});

// Or use the assembly-scanning overload
builder.Services.AddSencillaMessaging(typeof(OrdersMessagingConfig).Assembly);
```

---

## Message Attributes

### `[Stream]`

Route a message type to a specific stream/topic/queue:

```csharp
[Stream("orders.placed")]
public record OrderPlacedMessage(Guid OrderId, decimal Total);
```

### `[ExtendDispatcher]`

Extend the dispatcher with custom middleware for a specific message type.

### `[PayloadType]`

Specify the payload type for deserialization in polymorphic scenarios.

---

## Middleware Interface Reference

```csharp
public interface IMessageMiddleware
{
    Task HandleAsync<T>(
        Message<T> message,
        Func<Message<T>, CancellationToken, Task> next,
        CancellationToken token);
}
```

Example — retry middleware:

```csharp
[Implement]
public class RetryMiddleware : IMessageMiddleware
{
    public async Task HandleAsync<T>(
        Message<T> message,
        Func<Message<T>, CancellationToken, Task> next,
        CancellationToken token)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await next(message, token);
                return;
            }
            catch when (attempt < 2)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), token);
            }
        }
    }
}
```

---

## Coming Soon

- Kafka provider setup guide
- Azure Service Bus provider setup guide
- Transactional outbox pattern with EF Core
- Message serialization customization

---

## See Also

- [Commands & Events](../core/commands-events.md) — in-process CQRS (no broker)
- [Scheduler](../scheduler/README.md) — time-based message dispatch
- [Architecture](../architecture.md) — where messaging fits in the stack

---

[Home](../../README.md) / [Docs](../index.md) / **Messaging**
