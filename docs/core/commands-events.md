# Commands & Events (CQRS)

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Command/`, `libs/core/Event/`

Sencilla provides a lightweight **CQRS** (Command Query Responsibility Segregation) implementation. Commands and events are plain objects dispatched through typed dispatchers and processed by registered handlers.

---

## Core Concepts

| Concept | Interface | Description |
| ------- | --------- | ----------- |
| **Command** | `ICommand` / `ICommand<TResponse>` | An intent to change state. Has exactly one handler. |
| **Event** | `IEvent` | Something that happened. Can have zero or many handlers. |
| **Command dispatcher** | `ICommandDispatcher` | Routes commands to their handler. |
| **Event dispatcher** | `IEventDispatcher` | Broadcasts events to all registered handlers. |
| **Command handler** | `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` | Processes a specific command. |
| **Event handler** | `IEventHandler<TEvent>` | Processes a specific event type. |

---

## Commands

### `ICommand` / `ICommand<TResponse>`

```csharp
// Command with no return value
public interface ICommand
{
    Guid Id { get; }               // Unique command ID (auto-generated)
    Guid CorrelationId { get; }    // For tracking related operations
}

// Command that returns a result
public interface ICommand<TResponse> : ICommand { }
```

### Defining a Command

```csharp
// No return value
public record DeactivateUserCommand(Guid UserId) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CorrelationId { get; } = Guid.NewGuid();
}

// With return value
public record CreateProductCommand(string Name, decimal Price) : ICommand<Product>
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CorrelationId { get; } = Guid.NewGuid();
}
```

### `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>`

```csharp
// Handler for a command with no return value
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken token = default);
}

// Handler for a command that returns a result
public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken token = default);
}
```

### Implementing a Command Handler

```csharp
[Implement]  // ← auto-registers with DI
public class CreateProductHandler : ICommandHandler<CreateProductCommand, Product>
{
    private readonly ICreateRepository<Product, int> _products;

    public CreateProductHandler(ICreateRepository<Product, int> products)
        => _products = products;

    public async Task<Product> HandleAsync(CreateProductCommand cmd, CancellationToken token)
    {
        var product = new Product
        {
            Name = cmd.Name,
            Price = cmd.Price
        };
        return (await _products.Create(product, token))!;
    }
}
```

### `ICommandDispatcher` — Sending Commands

```csharp
public interface ICommandDispatcher
{
    Task Send(ICommand command, CancellationToken token = default);
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken token = default);
}
```

#### Usage in a controller or service

```csharp
// Inject ICommandDispatcher
public class ProductsController(ICommandDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var command = new CreateProductCommand(request.Name, request.Price);
        var product = await dispatcher.Send(command);
        return Ok(product);
    }
}
```

---

## Events

### `IEvent`

```csharp
public interface IEvent
{
    Guid Id { get; }              // Unique event ID
    Guid CorrelationId { get; }   // Links related operations
    string Type { get; }          // Event type name (string identifier)
}
```

### Defining an Event

```csharp
public record ProductPriceChangedEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public string Type => nameof(ProductPriceChangedEvent);

    public int ProductId { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
}
```

### `IEventHandler<TEvent>`

```csharp
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken token = default);
}
```

### Implementing an Event Handler

```csharp
[Implement]
public class PriceChangedNotificationHandler : IEventHandler<ProductPriceChangedEvent>
{
    private readonly IEmailService _email;

    public PriceChangedNotificationHandler(IEmailService email) => _email = email;

    public Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken token)
        => _email.SendPriceAlertAsync(@event.ProductId, @event.OldPrice, @event.NewPrice, token);
}

// Multiple handlers for the same event — all get called
[Implement]
public class PriceChangedAuditHandler : IEventHandler<ProductPriceChangedEvent>
{
    public Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken token)
    {
        // Log the change
        return Task.CompletedTask;
    }
}
```

### `IEventDispatcher` — Publishing Events

```csharp
public interface IEventDispatcher
{
    Task Publish(IEvent @event, CancellationToken token = default);
    Task Publish<TEvent>(TEvent @event, CancellationToken token = default) where TEvent : IEvent;
}
```

#### Usage

```csharp
[Implement]
public class UpdatePriceHandler : ICommandHandler<UpdatePriceCommand>
{
    private readonly IUpdateRepository<Product, int> _products;
    private readonly IEventDispatcher _events;

    public UpdatePriceHandler(
        IUpdateRepository<Product, int> products,
        IEventDispatcher events)
    {
        _products = products;
        _events = events;
    }

    public async Task HandleAsync(UpdatePriceCommand cmd, CancellationToken token)
    {
        var product = await _products.GetById(cmd.ProductId) ?? throw new NotFoundException();
        var oldPrice = product.Price;
        product.Price = cmd.NewPrice;
        await _products.Update(product, token);

        await _events.Publish(new ProductPriceChangedEvent
        {
            ProductId = cmd.ProductId,
            OldPrice = oldPrice,
            NewPrice = cmd.NewPrice
        }, token);
    }
}
```

---

## Middleware

Both command and event dispatchers support middleware — functions that wrap the dispatch pipeline:

```csharp
public interface ICommandMiddleware
{
    Task<TResponse> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<TResponse>> next,
        CancellationToken token);
}
```

### Example: Logging Middleware

```csharp
[Implement]
public class LoggingMiddleware : ICommandMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger) => _logger = logger;

    public async Task<TResponse> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<TResponse>> next,
        CancellationToken token)
    {
        _logger.LogInformation("Executing command {CommandType}", typeof(TCommand).Name);
        var result = await next(command, token);
        _logger.LogInformation("Completed command {CommandType}", typeof(TCommand).Name);
        return result;
    }
}
```

---

## Entity Events

The repository layer automatically publishes entity lifecycle events. See [Entities — Entity Events](entities.md#entity-events) for details.

---

## Integration with Messaging

Commands and events dispatched via `ICommandDispatcher` / `IEventDispatcher` run **in-process** by default. To publish to an external broker (RabbitMQ, Kafka, etc.), use `IMessageDispatcher` from `Sencilla.Messaging`. See [Messaging](../messaging/README.md).

---

## Pattern Summary

```text
Client
  │
  │  dispatcher.Send(new CreateProductCommand(...))
  ▼
ICommandDispatcher
  │  routes to the single registered handler
  ▼
ICommandHandler<CreateProductCommand, Product>
  │  business logic, calls repositories, raises events
  ▼
IEventDispatcher.Publish(new ProductCreatedEvent {...})
  │  broadcasts to all registered handlers
  ▼
IEventHandler<ProductCreatedEvent> × N handlers
```

---

## See Also

- [Entities — Entity Events](entities.md#entity-events) — automatic repository events
- [Dependency Injection](dependency-injection.md) — how `[Implement]` registers handlers
- [Messaging](../messaging/README.md) — cross-process event publishing
