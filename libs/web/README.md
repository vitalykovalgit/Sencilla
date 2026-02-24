# Sencilla.Web

ASP.NET Core web infrastructure for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla) — a modular .NET 10 application framework.

## What's Included

- **Zero-code CRUD controllers** — generate full REST APIs from a single `[CrudApi]` attribute on your entity
- **Base API controller** — `ApiController` with service resolution, exception mapping, and HTTP status helpers
- **Smart model binding** — automatic `Filter<TEntity>` binding from query strings and request bodies
- **Convention-based routing** — routes and authorization propagated from entity attributes
- **Response caching** — `[UseCaching]` attribute for in-memory caching with configurable TTL
- **DTO contracts** — `IDtoEntity<TEntity>` interface for entity-to-DTO mapping
- **File streaming** — `FileCallbackResult` for memory-efficient file downloads

## Installation

```bash
dotnet add package Sencilla.Web
```

## Quick Start

### 1. Define an entity with `[CrudApi]`

```csharp
using Sencilla.Core;
using Sencilla.Web;

[CrudApi(route: "api/products")]
public class Product :
    IEntity<int>,
    IEntityCreateable,   // → enables PUT endpoints
    IEntityUpdateable,   // → enables POST endpoints
    IEntityDeleteable     // → enables DELETE endpoints
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

This single attribute generates a full REST API — `GET`, `PUT`, `POST`, `DELETE` — with filtering, pagination, aggregation, upsert, merge, soft-delete, and batch operations. No controller class needed.

### 2. Register Sencilla.Web

```csharp
// Program.cs
var mvcBuilder = builder.Services.AddControllers();
builder.Services.AddSencilla(builder.Configuration);
builder.Services.AddSencillaWeb(mvcBuilder);
```

### 3. Use a custom controller (optional)

For custom endpoints, inherit from `ApiController`:

```csharp
public class ReportsController(IServiceProvider provider) : ApiController(provider)
{
    [HttpGet("reports/top-products")]
    public async Task<IActionResult> GetTopProducts()
        => await AjaxAction((IReadRepository<Product> repo)
            => repo.GetAll(new Filter<Product> { Take = 10, OrderBy = "Price", Descending = true }));
}
```

## Documentation

- [Web Module](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/web/README.md)
- [Getting Started](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/getting-started.md)
- [Core Repositories](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/repositories.md)
- [Filtering](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/filtering.md)

## License

MIT
