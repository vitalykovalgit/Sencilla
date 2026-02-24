# Sencilla.Core

Foundation package for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla) — a modular .NET 10 application framework.

## What's Included

- **Entity interfaces** — `IEntity<TKey>`, lifecycle markers (`IEntityCreateable`, `IEntityUpdateable`, `IEntityDeleteable`, etc.)
- **Repository pattern** — `IReadRepository`, `ICreateRepository`, `IUpdateRepository`, `IDeleteRepository`, and more
- **CQRS** — commands, queries, events, and dispatchers with middleware support
- **Auto-discovery DI** — attribute-based service registration (`[Implement]`, `[Singleton]`)
- **Filtering & pagination** — built-in filter, sort, and page abstractions
- **Exception hierarchy** — structured exceptions for validation, not-found, and authorization errors

## Installation

```bash
dotnet add package Sencilla.Core
```

## Quick Start

```csharp
using Sencilla.Core;

// Define an entity
public class Product : IEntity<int>, IEntityCreateable, IEntityUpdateable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

// Register services
builder.Services.AddSencilla(builder.Configuration);
```

## Documentation

- [Getting Started](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/getting-started.md)
- [Entities](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/entities.md)
- [Repositories](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/repositories.md)
- [Commands & Events](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/commands-events.md)
- [Dependency Injection](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/dependency-injection.md)

## License

MIT
