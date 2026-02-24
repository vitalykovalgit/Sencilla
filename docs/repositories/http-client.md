# HTTP Client Repository

**NuGet:** `Sencilla.Repository.HttpClient`
**Namespace:** `Sencilla.Repository.HttpClient`
**Source:** `libs/repositories/HttpClient/`

The HTTP Client repository implements the Sencilla repository interfaces by making HTTP calls to a remote REST API. Use it when:

- Your data source is a third-party or internal REST service
- You're building a BFF (Backend For Frontend) or API gateway
- You want to abstract a remote API behind the same interface your services already use

---

## Installation

```bash
dotnet add package Sencilla.Repository.HttpClient
```

---

## Class Hierarchy

```text
BaseRepository                       Holds WebContext (HTTP infrastructure)
├── ReadRepository<TEntity>          IReadRepository<TEntity, TKey>
├── CreateRepository<TEntity>        ICreateRepository<TEntity, TKey>
├── UpdateRepository<TEntity>        IUpdateRepository<TEntity, TKey>
├── DeleteRepository<TEntity>        IDeleteRepository<TEntity, TKey>
└── RemoveRepository<TEntity>        IRemoveRepository<TEntity, TKey>
```

---

## `ApiUrlAttribute`

Decorate your entity (or DTO) with `[ApiUrl]` to tell the repository the base URL for all operations on that type:

```csharp
[ApiUrl("https://api.example.com/products")]
public class Product : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

The repository maps CRUD operations to conventional REST endpoints:

| Operation | HTTP method | URL |
| --------- | ----------- | --- |
| `GetAll(filter)` | `GET` | `/products?skip=0&take=20&...` |
| `GetById(id)` | `GET` | `/products/{id}` |
| `Create(entity)` | `POST` | `/products` |
| `Update(entity)` | `PUT` | `/products/{id}` |
| `Delete(id)` | `DELETE` | `/products/{id}` |
| `Remove(id)` | `DELETE` | `/products/{id}/remove` |

---

## `SkipInUrlParamsAttribute`

Apply to entity properties that should **not** be serialized into the query string when building GET requests:

```csharp
[ApiUrl("https://api.example.com/products")]
public class Product : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [SkipInUrlParams]   // ← excluded from query params
    public string InternalMemo { get; set; } = string.Empty;
}
```

---

## Implementing a Repository

```csharp
[Implement]
public class RemoteProductRepository :
    ReadRepository<Product>,
    ICreateRepository<Product, int>,
    IUpdateRepository<Product, int>,
    IDeleteRepository<Product, int>
{
    public RemoteProductRepository(WebContext context) : base(context) { }
}
```

---

## `WebContext`

`WebContext` is the HTTP infrastructure container passed to all HTTP repositories. It holds the configured `HttpClient` and serialization settings.

Register it during DI setup:

```csharp
// Program.cs
builder.Services.AddHttpClient("SencillaHttpRepo", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
});

builder.Services.AddScoped<WebContext>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new WebContext(factory.CreateClient("SencillaHttpRepo"));
});
```

---

## Filter Serialization

`IFilter` properties are serialized into query string parameters:

```
GET /products?skip=0&take=20&search=widget&orderBy=Price&descending=true
```

Property filters are serialized as:
```
GET /products?properties[Price].type=LessOrEqual&properties[Price].values=100
```

> The remote API must understand this query string format. If it uses a different convention, override the `BuildUrl` method in your repository subclass.

---

## Custom URL Building

Override `BuildUrl` for non-standard REST APIs:

```csharp
[Implement]
public class LegacyProductRepository : ReadRepository<Product>
{
    public LegacyProductRepository(WebContext context) : base(context) { }

    protected override string BuildGetAllUrl(IFilter? filter)
    {
        var url = "https://legacy-api.example.com/api/v1/GetProducts";
        if (filter?.Take > 0)
            url += $"?pageSize={filter.Take}&pageNumber={filter.Skip / filter.Take + 1}";
        return url;
    }
}
```

---

## Combining HTTP and EF Core Repositories

You can mix backends in one application. Local entities are auto-registered with EF Core (no manual repository needed); remote entities use HTTP:

```csharp
// Local entity — auto-registered via EF Core (no repository class needed)
public class Order : IEntity<int>, IEntityCreateable { ... }

// Remote entity — HTTP (needs explicit implementation)
[Implement]
public class ExchangeRateRepository : ReadRepository<ExchangeRate>,
    IReadRepository<ExchangeRate, string>
{
    public ExchangeRateRepository(WebContext context) : base(context) { }
}

// Service injects both — same interface, different backends
public class PricingService(
    IReadRepository<Order, int> orders,
    IReadRepository<ExchangeRate, string> rates) { ... }
```

---

## See Also

- [Core Repository Interfaces](../core/repositories.md) — interface definitions
- [Entity Framework Repository](entity-framework.md) — local DB alternative
- [Filtering](../core/filtering.md) — `IFilter` serialization to query string
