# Web Module

**NuGet:** `Sencilla.Web`
**Namespace:** `Sencilla.Web`, `Microsoft.AspNetCore.Mvc`
**Source:** `libs/web/`

`Sencilla.Web` provides ASP.NET Core web infrastructure: base controllers, auto-generated CRUD APIs, smart model binding, response caching, and file streaming utilities.

---

## Installation

```bash
dotnet add package Sencilla.Web
```

---

## Setup

Register Sencilla.Web in your `Program.cs`:

```csharp
var mvcBuilder = builder.Services.AddControllers();
builder.Services.AddSencilla(builder.Configuration);
builder.Services.AddSencillaWeb(mvcBuilder);
```

`AddSencillaWeb` registers:

- Custom `FilterTypeBinderProvider` for automatic `Filter<TEntity>` binding
- `CrudApiControllerRouteConvention` for entity-driven routing
- `CrudApiControllerFeatureProvider` for automatic controller discovery
- JSON serializer configured to ignore reference cycles

---

## Features

| Feature | Description |
| ------- | ----------- |
| `[CrudApi]` attribute | Decorate an entity to generate a full REST CRUD controller — no code needed |
| `ApiController` | Base controller with service resolution, exception mapping, and HTTP helpers |
| `CrudApiController<T>` | Pre-built controller with 17+ endpoints: list, get, count, sum, min, max, average, create, update, upsert, merge, remove, undo, delete |
| `[UseCaching]` | In-memory response caching with configurable TTL |
| `IDtoEntity<T>` | DTO-to-entity mapping contract for web-facing models |
| Filter model binding | `Filter<TEntity>` auto-binds from query string or request body |
| `FileCallbackResult` | Stream file downloads without buffering |
| `[AllowSynchronousIO]` | Action filter to enable synchronous I/O per-endpoint |

---

## `[CrudApi]` — Zero-Code CRUD

Apply `[CrudApi]` to any entity that implements `IEntity<TKey>` to automatically generate a full REST controller at startup. No controller class is needed.

### Basic Usage

```csharp
using Sencilla.Core;
using Sencilla.Web;

[CrudApi(route: "api/products")]
public class Product :
    IEntity<int>,
    IEntityCreateable,
    IEntityUpdateable,
    IEntityDeleteable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### Generated Endpoints

This generates the following REST API for `Product`:

| Method | Route | Action | Repository |
| ------ | ----- | ------ | ---------- |
| `GET` | `/api/products` | List all (with filtering & pagination) | `IReadRepository<Product>` |
| `GET` | `/api/products/{id}` | Get by ID | `IReadRepository<Product>` |
| `GET` | `/api/products/count` | Get filtered count | `IReadRepository<Product>` |
| `GET` | `/api/products/sum` | Sum aggregation | `IReadRepository<Product>` |
| `GET` | `/api/products/max` | Max value | `IReadRepository<Product>` |
| `GET` | `/api/products/min` | Min value | `IReadRepository<Product>` |
| `GET` | `/api/products/avarage` | Average value | `IReadRepository<Product>` |
| `PUT` | `/api/products/{id}` | Create single | `ICreateRepository<Product>` |
| `PUT` | `/api/products` | Create batch | `ICreateRepository<Product>` |
| `POST` | `/api/products/{id}` | Update single | `IUpdateRepository<Product>` |
| `POST` | `/api/products` | Update batch | `IUpdateRepository<Product>` |
| `POST` | `/api/products/upsert/{id}` | Upsert single | `ICreateRepository<Product>` |
| `POST` | `/api/products/upsert` | Upsert batch | `ICreateRepository<Product>` |
| `POST` | `/api/products/merge/{id}` | Merge single | `ICreateRepository<Product>` |
| `POST` | `/api/products/merge` | Merge batch | `ICreateRepository<Product>` |
| `POST` | `/api/products/remove` | Soft-delete | `IRemoveRepository<Product>` |
| `POST` | `/api/products/undo` | Restore soft-deleted | `IRemoveRepository<Product>` |
| `DELETE` | `/api/products/{id}` | Delete by ID | `IDeleteRepository<Product>` |
| `DELETE` | `/api/products` | Delete batch by entities | `IDeleteRepository<Product>` |
| `DELETE` | `/api/products/ids` | Delete batch by IDs | `IDeleteRepository<Product>` |

### Attribute Parameters

```csharp
[CrudApi(route: "api/v1/products", cache: false)]
```

| Parameter | Type | Default | Description |
| --------- | ---- | ------- | ----------- |
| `route` | `string?` | `null` | Custom route template. When set, the entity name becomes the controller name |
| `cache` | `bool` | `false` | Enable caching flag (use `[UseCaching]` for configuration) |

### Authorization Propagation

The `[Authorize]` and `[AllowAnonymous]` attributes on the entity are automatically propagated to the generated controller:

```csharp
[CrudApi(route: "api/products")]
[Authorize]                       // → generated controller requires authentication
public class Product : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

```csharp
[CrudApi(route: "api/categories")]
[AllowAnonymous]                  // → generated controller allows anonymous access
public class Category : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

### Custom Key Types

`[CrudApi]` works with any key type. The key type is inferred from the `IEntity<TKey>` interface:

```csharp
[CrudApi(route: "api/orders")]
public class Order : IEntity<Guid>    // → CrudApiController<Order, Guid>
{
    public Guid Id { get; set; }
    public decimal Total { get; set; }
}
```

### How It Works

1. **Discovery** — `CrudApiControllerFeatureProvider` scans all loaded assemblies for types decorated with `[CrudApi]`
2. **Controller generation** — For each entity, a `CrudApiController<TEntity, TKey>` is dynamically registered in MVC
3. **Route convention** — `CrudApiControllerRouteConvention` reads the `Route` property from `[CrudApi]` and applies it to the controller
4. **Authorization** — `[Authorize]` and `[AllowAnonymous]` from the entity class are propagated as controller-level filters

---

## `ApiController` — Base Controller

All Sencilla controllers inherit from `ApiController`. It provides service resolution, HTTP status helpers, and exception-to-response mapping.

### Service Resolution

Use `R<TService>()` to resolve services from DI:

```csharp
public class ProductsController(IServiceProvider provider) : ApiController(provider)
{
    [HttpGet("products/featured")]
    public async Task<IActionResult> GetFeatured()
    {
        var repo = R<IReadRepository<Product>>();
        var products = await repo!.GetAll(new Filter<Product> { Take = 5 });
        return Ok(products);
    }
}
```

### AjaxAction — Safe Service Calls

`AjaxAction` wraps a service call with null-checking (returns 501 if not registered) and exception handling:

```csharp
public class OrdersController(IServiceProvider provider) : ApiController(provider)
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetAll(Filter<Order> filter, CancellationToken token)
        => await AjaxAction((IReadRepository<Order> repo) => repo.GetAll(filter, token));

    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetById(int id)
        => await AjaxAction((IReadRepository<Order> repo) => repo.GetById(id));

    [HttpPost("orders")]
    public async Task<IActionResult> Create([FromBody] Order order, CancellationToken token)
        => await AjaxAction((ICreateRepository<Order> repo) => repo.Create(order, token));
}
```

### HTTP Status Helpers

```csharp
NotImplemented("Feature not available");     // 501
ResourceGone("Resource deleted");            // 410
Forbidden("Access denied");                  // 403
Unauthorized("Login required");              // 401
InternalServerError(exception);              // 500
```

### Exception Mapping

`ExceptionToResponse` maps known exception types to appropriate HTTP status codes:

| Exception | HTTP Status |
| --------- | ----------- |
| `UnauthorizedException` | 401 Unauthorized |
| `ForbiddenException` | 403 Forbidden |
| Any other `Exception` | 500 Internal Server Error |

---

## `[UseCaching]` — Response Caching

Add `[UseCaching]` to an entity to cache `GetAll` results in memory:

```csharp
[CrudApi(route: "api/categories")]
[UseCaching(timeoutMinutes: 10)]
public class Category : IEntity<int>, IEntityCreateable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

| Parameter | Type | Default | Description |
| --------- | ---- | ------- | ----------- |
| `timeoutMinutes` | `int` | `5` | Cache TTL in minutes |

How it works:

- The `GetAll` endpoint in `CrudApiController` checks for `[UseCaching]` on the entity type
- If present, results are cached using `IMemoryCache` with a key based on the filter hash
- Subsequent requests with the same filter return cached results until the TTL expires

---

## Filter Model Binding

`Sencilla.Web` automatically binds `Filter<TEntity>` parameters from query strings and request bodies.

### Query String Binding

```http
GET /api/products?skip=0&take=20&search=widget&orderBy=Price&descending=true
```

```csharp
[HttpGet("products")]
public async Task<IActionResult> GetAll(Filter<Product> filter, CancellationToken token)
{
    var products = await repo.GetAll(filter, token);
    return Ok(products);
}
```

The binder maps:

- **Filter properties** (`Skip`, `Take`, `Search`, `OrderBy`, `Descending`) — bound to the `Filter` object directly
- **Entity properties** (`Name`, `Price`, etc.) — added as dynamic filter properties via `IFilter.AddProperty()`

### Request Body Binding

```json
POST /api/products/search
Content-Type: application/json

{
    "skip": 0,
    "take": 20,
    "orderBy": "Price",
    "descending": true,
    "name": "Widget"
}
```

Entity properties in the JSON body that don't match filter properties are extracted and added as dynamic filter criteria.

### Entity Property Filtering

You can filter by entity properties directly in the query string:

```http
GET /api/products?name=Widget&price=9.99
```

This binds `name` and `price` as typed filter properties, matching against the `Product` entity's property types.

---

## `IDtoEntity<TEntity>` — DTO Contracts

Use `IDtoEntity` to define web-facing DTOs with explicit entity mapping:

```csharp
public class ProductDto : IDtoEntity<Product>
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public Product ToEntity(Product entity)
    {
        entity.Id = Id;
        entity.Name = DisplayName;
        return entity;
    }

    public void FromEntity(Product entity)
    {
        Id = entity.Id;
        DisplayName = entity.Name;
    }
}
```

| Interface | Key Type |
| --------- | -------- |
| `IDtoEntity<TEntity>` | `int` (default) |
| `IDtoEntity<TEntity, TKey>` | Custom key type |

---

## `FileCallbackResult` — File Streaming

Stream file downloads without buffering the entire file in memory:

```csharp
[HttpGet("reports/{id}/download")]
public IActionResult DownloadReport(int id)
{
    return new FileCallbackResult(
        contentType: "application/pdf",
        fileDownloadName: $"report-{id}.pdf",
        callback: async stream =>
        {
            var file = await R<IFileService>()!.GetAsync(id);
            await file.CopyToAsync(stream);
        });
}
```

The callback receives the response body stream directly, allowing you to write data incrementally. The `Content-Disposition` header is set automatically from `fileDownloadName`.

---

## `[AllowSynchronousIO]` — Synchronous I/O

ASP.NET Core disables synchronous I/O by default. Use this action filter on endpoints that require it:

```csharp
[HttpPost("import")]
[AllowSynchronousIO]
public IActionResult Import()
{
    // Synchronous stream reads are allowed here
    using var reader = new StreamReader(Request.Body);
    var content = reader.ReadToEnd();
    return Ok();
}
```

---

## Full Example

A complete setup with auto-generated CRUD, caching, authorization, and a custom controller:

```csharp
// === Entities ===

[CrudApi(route: "api/products")]
[Authorize]
public class Product : IEntity<int>, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}

[CrudApi(route: "api/categories")]
[AllowAnonymous]
[UseCaching(timeoutMinutes: 30)]
public class Category : IEntity<int>, IEntityCreateable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// === Custom Controller ===

public class DashboardController(IServiceProvider provider) : ApiController(provider)
{
    [HttpGet("api/dashboard/stats")]
    public async Task<IActionResult> GetStats(CancellationToken token)
    {
        var productRepo = R<IReadRepository<Product>>()!;
        var categoryRepo = R<IReadRepository<Category>>()!;

        var productCount = await productRepo.GetCount(new Filter<Product>(), token);
        var categoryCount = await categoryRepo.GetCount(new Filter<Category>(), token);

        return Ok(new { Products = productCount, Categories = categoryCount });
    }

    [HttpGet("api/dashboard/export")]
    public IActionResult Export()
    {
        return new FileCallbackResult("text/csv", "export.csv", async stream =>
        {
            var writer = new StreamWriter(stream);
            var products = await R<IReadRepository<Product>>()!.GetAll();
            await writer.WriteLineAsync("Id,Name,Price");
            foreach (var p in products)
                await writer.WriteLineAsync($"{p.Id},{p.Name},{p.Price}");
            await writer.FlushAsync();
        });
    }
}

// === Program.cs ===

var builder = WebApplication.CreateBuilder(args);

var mvcBuilder = builder.Services.AddControllers();
builder.Services.AddSencilla(builder.Configuration);
builder.Services.AddSencillaRepositoryForEF(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSencillaWeb(mvcBuilder);

var app = builder.Build();
app.MapControllers();
app.Run();
```

This generates 20+ REST endpoints for `Product` and `Category` automatically, plus the custom dashboard endpoints.

---

## API Reference

### Classes

| Class | Namespace | Description |
| ----- | --------- | ----------- |
| `ApiController` | `Microsoft.AspNetCore.Mvc` | Base controller with service resolution and HTTP helpers |
| `CrudApiController<TEntity>` | `Microsoft.AspNetCore.Mvc` | Auto-generated CRUD controller (int key) |
| `CrudApiController<TEntity, TKey>` | `Microsoft.AspNetCore.Mvc` | Auto-generated CRUD controller (custom key) |
| `FileCallbackResult` | `Sencilla.Web` | Stream file downloads via callback |
| `CrudApiControllerFeatureProvider` | `Sencilla.Web` | Discovers `[CrudApi]` entities and registers controllers |
| `CrudApiControllerRouteConvention` | `Sencilla.Web` | Applies routes and auth from entity attributes to controllers |
| `FilterTypeBinder` | `Sencilla.Web` | Binds `Filter<T>` from query strings |
| `FilterTypeBodyBinder` | `Sencilla.Web` | Binds `Filter<T>` from JSON request bodies |
| `FilterTypeBinderProvider` | `Sencilla.Web` | Routes filter parameters to the correct binder |

### Attributes

| Attribute | Target | Description |
| --------- | ------ | ----------- |
| `[CrudApi(route, cache)]` | Class | Generates a CRUD controller for the entity |
| `[UseCaching(timeoutMinutes)]` | Class | Enables in-memory caching for `GetAll` queries |
| `[AllowSynchronousIO]` | Method | Enables synchronous I/O for the action |

### Interfaces

| Interface | Description |
| --------- | ----------- |
| `IDtoEntity<TEntity>` | DTO mapping contract with `int` key |
| `IDtoEntity<TEntity, TKey>` | DTO mapping contract with custom key type |

### Extension Methods

| Method | Description |
| ------ | ----------- |
| `AddSencillaWeb(IServiceCollection, IMvcBuilder)` | Registers all Sencilla.Web services, binders, conventions, and feature providers |

---

## See Also

- [Web API (Minimal API)](../webapi/README.md) — alternative to MVC controllers
- [Core Repository Interfaces](../core/repositories.md) — repositories injected into controllers
- [Filtering](../core/filtering.md) — `Filter` and `IFilter` details
- [Getting Started](../getting-started.md) — framework setup guide
