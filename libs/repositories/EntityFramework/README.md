# Sencilla.Repository.EntityFramework

Entity Framework Core repository implementation for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla).

## What's Included

- Full CRUD repository implementation backed by EF Core
- Support for all Sencilla entity lifecycle interfaces (create, update, soft-delete, hide)
- Built-in filtering, sorting, and pagination
- Transaction support
- Zero-boilerplate auto-registration — define an entity, get repositories automatically

## Installation

```bash
dotnet add package Sencilla.Repository.EntityFramework
```

## Quick Start

**1. Define an entity** — the interfaces you implement determine which repositories are auto-registered:

```csharp
using Sencilla.Core;

public class Product :
    IEntity<int>,
    IEntityCreateableTrack,   // → ICreateRepository<Product> + auto-sets CreatedDate
    IEntityUpdateableTrack,   // → IUpdateRepository<Product> + auto-sets UpdatedDate
    IEntityDeleteable         // → IDeleteRepository<Product>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
```

**2. Register Sencilla** — no DbContext or repository classes needed:

```csharp
builder.Services.AddSencilla(builder.Configuration);
builder.Services.AddSencillaRepositoryForEF(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

**3. Inject and use:**

```csharp
public class ProductService(
    IReadRepository<Product> reader,
    ICreateRepository<Product> creator,
    IUpdateRepository<Product> updater)
{
    // All repositories are auto-injected
}
```

## Documentation

- [Entity Framework Repository](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/repositories/entity-framework.md)

## License

MIT
