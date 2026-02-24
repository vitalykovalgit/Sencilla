# Web Module

**NuGet:** `Sencilla.Web`
**Namespace:** `Sencilla.Web`
**Source:** `libs/web/`

> **Status:** Documentation in progress.

`Sencilla.Web` provides ASP.NET Core web infrastructure: base controllers, auto-generated CRUD APIs, and custom model binders.

---

## Features

| Feature | Description |
| ------- | ----------- |
| `ApiController` | Base controller with common helpers |
| `CrudApiController` | Auto-generated CRUD endpoints from a single attribute |
| `[CrudApi]` attribute | Decorate an entity to get full REST CRUD with no controller code |
| `IWebEntity` | Marker interface for web-facing entities |
| Filter model binding | `IFilter` binds from query string automatically |
| `FileCallbackResult` | Stream file downloads |
| `[UseCaching]` | Response caching attribute |

---

## `[CrudApi]` — Zero-Code CRUD

Apply `[CrudApi]` to an entity to generate `GET`, `POST`, `PUT`, `DELETE` endpoints automatically:

```csharp
[CrudApi]
public class Product : IEntity<int>, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
```

Register CRUD API generation:

```csharp
// Program.cs
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new CrudApiControllerRouteConvention());
})
.ConfigureApplicationPartManager(manager =>
{
    manager.FeatureProviders.Add(new CrudApiControllerFeatureProvider());
});
```

This generates the following endpoints for `Product`:

| Method | URL | Description |
| ------ | --- | ----------- |
| `GET` | `/api/products` | List all (with `IFilter`) |
| `GET` | `/api/products/{id}` | Get by ID |
| `POST` | `/api/products` | Create |
| `PUT` | `/api/products/{id}` | Update |
| `DELETE` | `/api/products/{id}` | Delete |

---

## `ApiController` — Base Controller

All Sencilla controllers inherit from `ApiController`:

```csharp
public class ProductsController : ApiController
{
    private readonly IReadRepository<Product, int> _products;

    public ProductsController(IReadRepository<Product, int> products)
        => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Filter filter)
        => Ok(await _products.GetAll(filter));
}
```

---

## Filter Model Binding

`FilterTypeBinderProvider` is registered automatically by `AddSencilla()`. It allows `IFilter` / `Filter` to bind from query string:

```
GET /api/products?skip=0&take=20&search=widget&orderBy=Price&descending=true
```

```csharp
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] Filter filter)
{
    var products = await _repo.GetAll(filter);
    return Ok(products);
}
```

---

## File Downloads — `FileCallbackResult`

Stream large file responses without buffering in memory:

```csharp
[HttpGet("{id}/download")]
public IActionResult Download(int id)
{
    return new FileCallbackResult("application/pdf", async (stream, context) =>
    {
        var file = await _files.GetAsync(id);
        await file.CopyToAsync(stream);
    })
    {
        FileDownloadName = $"document-{id}.pdf"
    };
}
```

---

## Coming Soon

- Full `ApiController` method reference
- `[CrudApi]` customization options (route prefix, authorization, excluded methods)
- `[UseCaching]` configuration
- `IWebEntity` integration with serialization

---

## See Also

- [Web API (Minimal API)](../webapi/README.md) — alternative to MVC controllers
- [Core Repository Interfaces](../core/repositories.md) — injected into controllers
- [Filtering](../core/filtering.md) — `Filter` model binding details
