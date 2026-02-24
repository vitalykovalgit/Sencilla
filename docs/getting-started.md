# Getting Started

This guide walks you through setting up a new .NET project that uses Sencilla, creating your first entity, implementing a repository, and exposing it via an API endpoint.

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

At minimum you need `Sencilla.Core`. Add the repository implementation you plan to use:

```bash
# Core (always required)
dotnet add package Sencilla.Core

# Entity Framework repository implementation
dotnet add package Sencilla.Repository.EntityFramework

# If using minimal API pattern
dotnet add package Sencilla.Web.MinimalApi
```

---

## Step 3 — Define your entity

Entities implement `IEntity<TKey>` and any lifecycle marker interfaces they need.

```csharp
// Models/Product.cs
using Sencilla.Core;

public class Product : IEntity<int>, IEntityCreatable, IEntityUpdatable
{
    // Required by IEntity<int>
    public int Id { get; set; }

    // Your domain properties
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }

    // Required by IEntityCreatable
    public DateTime CreatedAt { get; set; }

    // Required by IEntityUpdatable
    public DateTime UpdatedAt { get; set; }
}
```

> **What do lifecycle interfaces do?**
> They are marker interfaces that signal intent. The repository layer and event system use them to know which operations are valid for a given entity and which events to fire.
> See [Entities](core/entities.md) for the full list.

---

## Step 4 — Create the DbContext

```csharp
// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}
```

---

## Step 5 — Implement the repository

```csharp
// Data/ProductRepository.cs
using Sencilla.Core;
using Sencilla.Repository.EntityFramework;

[Implement]  // ← auto-registers this class with the DI container
public class ProductRepository :
    ReadRepository<Product, AppDbContext>,
    ICreateRepository<Product, int>,
    IUpdateRepository<Product, int>,
    IDeleteRepository<Product, int>
{
    public ProductRepository(RepositoryDependency<AppDbContext> dep) : base(dep) { }
}
```

The `[Implement]` attribute tells Sencilla's auto-discovery to register this class. You do **not** need a manual `services.AddScoped<...>()` call.

`ReadRepository<TEntity, TContext>` provides the full read API (`GetAll`, `GetById`, `Query`, etc.) out of the box. Add `ICreateRepository<TEntity, TKey>` etc. to enable create/update/delete.

---

## Step 6 — Register Sencilla

```csharp
// Program.cs
using Sencilla.Core;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Register Sencilla — scans this assembly (and Sencilla assemblies) for [Implement] types
builder.Services.AddSencilla(typeof(Program).Assembly);

var app = builder.Build();
```

---

## Step 7 — Use the repository

### Via Minimal API

```csharp
// Program.cs (continued)

app.MapGet("/products", async (IReadRepository<Product, int> repo) =>
{
    var products = await repo.GetAll();
    return Results.Ok(products);
});

app.MapGet("/products/{id:int}", async (int id, IReadRepository<Product, int> repo) =>
{
    var product = await repo.GetById(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapPost("/products", async (Product product, ICreateRepository<Product, int> repo) =>
{
    var created = await repo.Create(product);
    return Results.Created($"/products/{created!.Id}", created);
});

app.MapPut("/products/{id:int}", async (int id, Product product, IUpdateRepository<Product, int> repo) =>
{
    product.Id = id;
    var updated = await repo.Update(product);
    return Results.Ok(updated);
});

app.MapDelete("/products/{id:int}", async (int id, IDeleteRepository<Product, int> repo) =>
{
    await repo.Delete(id);
    return Results.NoContent();
});

app.Run();
```

### Via a service class

```csharp
// Services/ProductService.cs
public class ProductService
{
    private readonly IReadRepository<Product, int> _read;
    private readonly ICreateRepository<Product, int> _create;

    public ProductService(
        IReadRepository<Product, int> read,
        ICreateRepository<Product, int> create)
    {
        _read = read;
        _create = create;
    }

    public Task<IEnumerable<Product>> GetAvailableAsync()
    {
        var filter = new Filter();
        filter.AddProperty("IsAvailable", FilterPropertyType.Bool, true);
        return _read.GetAll(filter);
    }
}
```

---

## Step 8 — Apply filtering and pagination

Inject `IFilter` in an API endpoint to get built-in pagination, sorting, and search:

```csharp
app.MapGet("/products", async (
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20,
    [FromQuery] string? search = null,
    IReadRepository<Product, int> repo) =>
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
