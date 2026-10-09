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

## Client error log

`services.AddClientLogging()` maps `POST /api/v1/client-log` (anonymous) through `MapSencillaEndpoints()`. A browser posts `{ kind, message, stack, path, build, window_class, input_type, extra }` as `text/plain`, and each report becomes one `client-error …` warning in the server logs. Untrusted input: 16 KB at most (413), JSON or 400, 120 reports a minute per process (429), every field truncated and logged as a parameter.

## Documentation

- [Web API](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/webapi/README.md)

## License

MIT
