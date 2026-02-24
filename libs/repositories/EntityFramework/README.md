# Sencilla.Repository.EntityFramework

Entity Framework Core repository implementation for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla).

## What's Included

- Full CRUD repository implementation backed by EF Core
- Support for all Sencilla entity lifecycle interfaces (create, update, soft-delete, hide)
- Built-in filtering, sorting, and pagination
- Transaction support
- Auto-discovery service registration

## Installation

```bash
dotnet add package Sencilla.Repository.EntityFramework
```

## Quick Start

```csharp
using Sencilla.Core;
using Sencilla.Repository.EntityFramework;

[Implement]
public class ProductRepository :
    ReadRepository<Product, AppDbContext>,
    ICreateRepository<Product, int>,
    IUpdateRepository<Product, int>,
    IDeleteRepository<Product, int>
{
    public ProductRepository(RepositoryDependency<AppDbContext> dep) : base(dep) { }
}
```

## Documentation

- [Entity Framework Repository](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/repositories/entity-framework.md)

## License

MIT
