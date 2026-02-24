# Web API — Minimal API

**NuGet:** `Sencilla.Web.MinimalApi`
**Namespace:** `Sencilla.Web.MinimalApi`
**Source:** `libs/webapi/MinimalApi/`

> **Status:** Documentation in progress.

`Sencilla.Web.MinimalApi` provides a structured way to organize [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis) using the `IEndpoint` pattern.

---

## `IEndpoint` — The Core Abstraction

```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

Each endpoint (or group of related endpoints) implements `IEndpoint` and maps its routes in `MapEndpoint`.

---

## Quick Start

### 1. Install

```bash
dotnet add package Sencilla.Web.MinimalApi
```

### 2. Implement an endpoint

```csharp
[Implement]
public class ProductEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
    }

    private static async Task<IResult> GetAll(
        [FromQuery] int skip,
        [FromQuery] int take,
        IReadRepository<Product, int> repo)
    {
        var filter = new Filter { Skip = skip, Take = take };
        return Results.Ok(await repo.GetAll(filter));
    }

    private static async Task<IResult> GetById(
        int id,
        IReadRepository<Product, int> repo)
    {
        var product = await repo.GetById(id);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> Create(
        Product product,
        ICreateRepository<Product, int> repo)
    {
        var created = await repo.Create(product);
        return Results.Created($"/api/products/{created!.Id}", created);
    }

    private static async Task<IResult> Update(
        int id, Product product,
        IUpdateRepository<Product, int> repo)
    {
        product.Id = id;
        return Results.Ok(await repo.Update(product));
    }

    private static async Task<IResult> Delete(
        int id,
        IDeleteRepository<Product, int> repo)
    {
        await repo.Delete(id);
        return Results.NoContent();
    }
}
```

### 3. Register all endpoints

```csharp
// Program.cs
builder.Services.AddSencilla(builder.Configuration);

var app = builder.Build();
app.MapSencillaEndpoints();   // discovers and maps all IEndpoint implementations
```

---

## Built-in Endpoint Bases

Sencilla provides generic base endpoints for common CRUD patterns:

```csharp
// Pre-built GET endpoint
public class Get<TEntity> : IEndpoint where TEntity : IBaseEntity { ... }

// Pre-built POST endpoint
public class Post<TEntity> : IEndpoint where TEntity : IBaseEntity { ... }

// Pre-built PUT endpoint
public class Put<TEntity> : IEndpoint where TEntity : IBaseEntity { ... }

// Pre-built DELETE endpoint
public class Delete<TEntity> : IEndpoint where TEntity : IBaseEntity { ... }
```

Use them for even less boilerplate:

```csharp
[Implement]
public class ProductGetEndpoint : Get<Product> { }

[Implement]
public class ProductPostEndpoint : Post<Product> { }
```

---

## `AllowSynchronousIOFilter`

When you need synchronous I/O in a minimal API endpoint (e.g., PDF generation), apply this filter to suppress the ASP.NET Core warning:

```csharp
group.MapGet("/export", ExportHandler)
     .AddEndpointFilter<AllowSynchronousIOFilter>();
```

---

## Coming Soon

- Authorization integration
- OpenAPI/Swagger configuration for endpoint groups
- Response type documentation via `Produces<T>()`
- Endpoint versioning

---

## See Also

- [Web Module](../web/README.md) — MVC controller alternative
- [Core Repository Interfaces](../core/repositories.md) — injected into endpoints
- [Getting Started](../getting-started.md) — minimal API setup example
