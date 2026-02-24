# Getting Started

This guide walks you through setting up a new .NET project with Sencilla, defining an entity, and exposing it via API endpoints — with zero repository or DbContext boilerplate.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later
- Any IDE — Visual Studio 2022, Rider, or VS Code with C# DevKit

---

## Step 1 — Create a new project

```bash
dotnet new webapi -n MyApp
cd MyApp
```

---

## Step 2 — Add Sencilla packages

```bash
# Core (always required)
dotnet add package Sencilla.Core

# Entity Framework repository implementation
dotnet add package Sencilla.Repository.EntityFramework
```

---

## Step 3 — Define your entity

Entities implement `IEntity<TKey>` and lifecycle marker interfaces. The interfaces you add determine which repositories are **automatically registered** for the entity — no manual repository classes needed.

```csharp
// Models/Product.cs
using Sencilla.Core;

public class Product :
    IEntity<int>,
    IEntityCreateableTrack,   // → auto-registers ICreateRepository<Product> + auto-sets CreatedDate
    IEntityUpdateableTrack,   // → auto-registers IUpdateRepository<Product> + auto-sets UpdatedDate
    IEntityDeleteable         // → auto-registers IDeleteRepository<Product>
{
    // Required by IEntity<int>
    public int Id { get; set; }

    // Your domain properties
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }

    // Required by IEntityCreateableTrack
    public DateTime CreatedDate { get; set; }

    // Required by IEntityUpdateableTrack
    public DateTime UpdatedDate { get; set; }
}
```

> **What do lifecycle interfaces do?**
> They are marker interfaces that signal intent. Sencilla scans your entities at startup, auto-registers the matching repository implementations, and wires up lifecycle events (e.g. auto-setting `CreatedDate` on insert).
> See [Entities](core/entities.md) for the full list.

---

## Step 4 — Register Sencilla

```csharp
// Program.cs
using Sencilla.Core;

var builder = WebApplication.CreateBuilder(args);

// Register Sencilla core — scans assemblies for auto-discovery
builder.Services.AddSencilla(builder.Configuration);

// Register EF Core repositories — entities are auto-discovered, DynamicDbContext is auto-configured
builder.Services.AddSencillaRepositoryForEF(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();
```

That's it. No `DbContext` class, no repository classes. Sencilla:

1. Discovers all entities implementing `IEntity<TKey>`
2. Builds a `DynamicDbContext` with model mappings for each entity
3. Registers `IReadRepository<Product>`, `ICreateRepository<Product>`, `IUpdateRepository<Product>`, `IDeleteRepository<Product>` automatically

---

## Step 5 — Use the repository

### Via Minimal API

```csharp
// Program.cs (continued)

app.MapGet("/products", async (IReadRepository<Product> repo) =>
{
    var products = await repo.GetAll();
    return Results.Ok(products);
});

app.MapGet("/products/{id:int}", async (int id, IReadRepository<Product> repo) =>
{
    var product = await repo.GetById(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapPost("/products", async (Product product, ICreateRepository<Product> repo) =>
{
    var created = await repo.Create(product);
    return Results.Created($"/products/{created!.Id}", created);
});

app.MapPut("/products/{id:int}", async (int id, Product product, IUpdateRepository<Product> repo) =>
{
    product.Id = id;
    var updated = await repo.Update(product);
    return Results.Ok(updated);
});

app.MapDelete("/products/{id:int}", async (int id, IDeleteRepository<Product> repo) =>
{
    await repo.Delete(id);
    return Results.NoContent();
});

app.Run();
```

### Via a service class

```csharp
// Services/ProductService.cs
public class ProductService(
    IReadRepository<Product> read,
    ICreateRepository<Product> create)
{
    public Task<IEnumerable<Product>> GetAvailableAsync()
    {
        var filter = new Filter();
        filter.AddProperty("IsAvailable", typeof(bool), true);
        return read.GetAll(filter);
    }
}
```

---

## Step 6 — Apply filtering and pagination

Build an `IFilter` to get built-in pagination, sorting, and search:

```csharp
app.MapGet("/products", async (
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20,
    [FromQuery] string? search = null,
    IReadRepository<Product> repo) =>
{
    var filter = new Filter
    {
        Skip = skip,
        Take = take,
        Search = search
    };
    var products = await repo.GetAll(filter);
    var count = await repo.GetCount(filter);
    return Results.Ok(new { data = products, total = count });
});
```

See [Filtering](core/filtering.md) for the full `IFilter` API.

---

## Next Steps

| Topic | Link |
| ----- | ---- |
| Entity lifecycle events | [Entities](core/entities.md) |
| Full repository API reference | [Repository Interfaces](core/repositories.md) |
| Filtering, sorting, pagination | [Filtering](core/filtering.md) |
| CQRS — commands and events | [Commands & Events](core/commands-events.md) |
| DI attributes and lifetime control | [Dependency Injection](core/dependency-injection.md) |
| EF Core repository internals | [Entity Framework Repository](repositories/entity-framework.md) |
| Message-driven architecture | [Messaging](messaging/README.md) |
| Background tasks | [Scheduler](scheduler/README.md) |
