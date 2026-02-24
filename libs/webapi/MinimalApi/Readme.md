# Sencilla.Web.MinimalApi

Minimal API endpoint abstractions for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla). Map CRUD operations to minimal API endpoints with minimal boilerplate.

## Installation

```bash
dotnet add package Sencilla.Web.MinimalApi
```

## Quick Start

```csharp
app.MapGet("/products", async (IReadRepository<Product, int> repo) =>
    await repo.GetAll());

app.MapPost("/products", async (Product product, ICreateRepository<Product, int> repo) =>
    await repo.Create(product));
```

## Documentation

- [Web API](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/webapi/README.md)

## License

MIT
