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
    options.UseInMemoryQueue();  // swap to UseRabbitMQ(), UseKafka(), etc.
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

## Middleware

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

## Configuration Reference

```json
{
  "Messaging": {
    "Provider": "RabbitMQ",
    "Consumers": [
      { "Queue": "orders.placed", "HandlerType": "OrderPlacedHandler" }
    ],
    "Routes": [
      { "MessageType": "OrderPlacedMessage", "Stream": "orders.placed" }
    ],
    "Streams": [
      { "Name": "orders.placed", "Durable": true, "AutoDelete": false }
    ]
  }
}
```

---

## Coming Soon

- RabbitMQ provider setup guide
- Kafka provider setup guide
- Azure Service Bus provider setup guide
- Transactional outbox pattern with EF Core
- Dead letter queue handling
- Message serialization customization

---

## See Also

- [Commands & Events](../core/commands-events.md) — in-process CQRS (no broker)
- [Scheduler](../scheduler/README.md) — time-based message dispatch
- [Architecture](../architecture.md) — where messaging fits in the stack

---

[Home](../../README.md) / [Docs](../index.md) / **Messaging**
