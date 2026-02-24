# Core Module — `Sencilla.Core`

**NuGet:** `Sencilla.Core`
**Namespace:** `Sencilla.Core`
**Source:** `libs/core/`

`Sencilla.Core` is the foundation of the entire framework. Every other Sencilla package depends on it. It contains **no external NuGet dependencies** beyond ASP.NET Core abstractions.

---

## What's Inside

| Area | Description | Doc |
| ---- | ----------- | --- |
| **Entities** | `IEntity<TKey>`, lifecycle marker interfaces, entity events | [entities.md](entities.md) |
| **Repositories** | `IReadRepository`, `ICreateRepository`, the full interface hierarchy | [repositories.md](repositories.md) |
| **Filtering** | `IFilter` — pagination, sorting, search, dynamic property filters | [filtering.md](filtering.md) |
| **Commands & Events** | CQRS — `ICommand`, `IEvent`, dispatchers, handlers | [commands-events.md](commands-events.md) |
| **Dependency Injection** | `[Implement]`, `[Singleton]`, auto-discovery, `StarterKit` | [dependency-injection.md](dependency-injection.md) |
| **Exceptions** | HTTP-mapped exception hierarchy | [exceptions.md](exceptions.md) |
| **Application** | `ISencillaApp`, `SencillaApp` — service provider abstraction | (below) |
| **Serialization** | Custom JSON converters for arrays/objects | (below) |
| **Extensions** | `ExpressionEx`, `IEnumerableEx`, `StringEx` utilities | (below) |

---

## Installation

```bash
dotnet add package Sencilla.Core
```

---

## Quick Reference

### Register everything

```csharp
// Program.cs
builder.Services.AddSencilla(typeof(Program).Assembly);
```

This single call:
- Scans for `[AutoDiscovery]`-marked assemblies
- Registers all classes decorated with `[Implement]`, `[Singleton]`, `[PerRequest]`
- Wires up command and event dispatchers
- Sets up the `ISencillaApp` service provider

### Define an entity

```csharp
public class Order : IEntity<Guid>, IEntityCreatable, IEntityUpdatable, IEntityRemoveable
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsRemoved { get; set; }  // soft delete
}
```

### Use a repository

```csharp
// Inject by interface — not by concrete type
public class OrderService(IReadRepository<Order, Guid> orders)
{
    public Task<IEnumerable<Order>> GetRecentAsync() =>
        orders.GetAll(new Filter { Take = 10, OrderBy = ["CreatedAt"], Descending = true });
}
```

### Dispatch a command

```csharp
public record CreateOrderCommand(string CustomerName, decimal Total) : ICommand<Order>;

[Implement]
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly ICreateRepository<Order, Guid> _repo;

    public CreateOrderHandler(ICreateRepository<Order, Guid> repo) => _repo = repo;

    public async Task<Order> HandleAsync(CreateOrderCommand cmd, CancellationToken token)
    {
        var order = new Order { CustomerName = cmd.CustomerName, Total = cmd.Total };
        return (await _repo.Create(order, token))!;
    }
}
```

---

## Application Service — `ISencillaApp`

`ISencillaApp` is a lightweight service-locator abstraction for framework-internal use. Avoid using it in application code; prefer constructor injection.

```csharp
public interface ISencillaApp
{
    T Provide<T>() where T : notnull;
}
```

---

## Serialization

Two custom JSON converters handle non-standard types:

| Converter | Purpose |
| --------- | ------- |
| `JsonArrayConverter` | Deserializes JSON arrays into typed collections |
| `JsonObjectConverter` | Deserializes JSON objects with flexible key types |

Register via attribute or in `JsonSerializerOptions`:

```csharp
[JsonConverter(typeof(JsonArrayConverter))]
public List<string> Tags { get; set; } = [];
```

---

## Extension Methods

### `ExpressionEx`

Utilities for combining LINQ expressions:

```csharp
// Combine two predicates with AND
Expression<Func<T, bool>> combined = expr1.And(expr2);

// Combine with OR
Expression<Func<T, bool>> combined = expr1.Or(expr2);
```

### `IEnumerableEx`

```csharp
// Check if a collection is null or empty
if (myList.IsNullOrEmpty()) { ... }

// ForEach with index
items.ForEachIndex((item, i) => Console.WriteLine($"{i}: {item}"));
```

### `StringEx`

```csharp
// Null-safe operations
string safe = myString.OrEmpty();
bool hasValue = myString.HasValue();   // !string.IsNullOrWhiteSpace
```

---

## System Variables — `ISystemVariable`

Used internally to access typed configuration entries:

```csharp
public interface ISystemVariable
{
    string Key { get; }
    string? Value { get; }
    T Get<T>();
}
```

---

## See Also

- [Getting Started](../getting-started.md)
- [Architecture](../architecture.md)
- [Entity Framework Repository](../repositories/entity-framework.md) — concrete implementation of core interfaces
