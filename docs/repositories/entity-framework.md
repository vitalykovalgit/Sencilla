# Entity Framework Core Repository

[Home](../../README.md) / [Docs](../index.md) / [Repositories](README.md) / Entity Framework

**NuGet:** `Sencilla.Repository.EntityFramework`
**Namespace:** `Sencilla.Repository.EntityFramework`
**Source:** `libs/repositories/EntityFramework/`

**Dependencies:** `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `System.Linq.Dynamic.Core`, `Sencilla.Core`

The EF Core repository is the **primary data access implementation**. It uses a `DynamicDbContext` that auto-discovers your entities and provides the full Sencilla repository interface hierarchy — with zero boilerplate.

---

## Installation

```bash
dotnet add package Sencilla.Repository.EntityFramework
```

---

## How It Works

Sencilla **auto-registers** repository implementations based on the lifecycle interfaces your entity implements. You do not need to create repository classes, DbContext classes, or manual DI registrations.

```text
Entity implements IEntityCreateable
    → ICreateRepository<TEntity> auto-registered → backed by CreateRepository<TEntity, DynamicDbContext>

Entity implements IEntityUpdateable
    → IUpdateRepository<TEntity> auto-registered → backed by UpdateRepository<TEntity, DynamicDbContext>

Entity implements IEntityDeleteable
    → IDeleteRepository<TEntity> auto-registered → backed by DeleteRepository<TEntity, DynamicDbContext>

Entity implements IEntityRemoveable
    → IRemoveRepository<TEntity> auto-registered → backed by RemoveRepository<TEntity, DynamicDbContext>

All entities with IEntity<TKey>
    → IReadRepository<TEntity> always auto-registered
```

---

## Quick Setup

### 1. Define the entity

```csharp
public class Product :
    IEntity<int>,
    IEntityCreateableTrack,   // auto-registers ICreateRepository<Product> + auto-sets CreatedDate
    IEntityUpdateableTrack,   // auto-registers IUpdateRepository<Product> + auto-sets UpdatedDate
    IEntityDeleteable         // auto-registers IDeleteRepository<Product>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
```

### 2. Register Sencilla

```csharp
// Program.cs
builder.Services.AddSencilla(builder.Configuration);
builder.Services.AddSencillaRepositoryForEF(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

That's it. No `DbContext`, no repository classes. Sencilla's `DynamicDbContext` auto-discovers your entities and builds the EF model. All matching `IReadRepository<Product>`, `ICreateRepository<Product>`, etc. are registered automatically.

### 3. Inject and use

```csharp
public class ProductService(
    IReadRepository<Product> reader,
    ICreateRepository<Product> creator,
    IUpdateRepository<Product> updater)
{
    public async Task<IEnumerable<Product>> GetAllProducts()
        => await reader.GetAll();

    public async Task<Product?> CreateProduct(Product product)
        => await creator.Create(product);
}
```

---

## Class Hierarchy

```text
Resolveable
└── BaseRepository<TContext>                      Holds DbContext + RepositoryDependency
    └── ReadRepository<TEntity, TContext>          IReadRepository<TEntity, TKey>
        ├── CreateRepository<TEntity, TContext>    + ICreateRepository
        ├── UpdateRepository<TEntity, TContext>    + IUpdateRepository
        ├── DeleteRepository<TEntity, TContext>    + IDeleteRepository
        ├── RemoveRepository<TEntity, TContext>    + IRemoveRepository
        └── HideRepository<TEntity, TContext>      + IHideRepository
```

---

## `DynamicDbContext`

Sencilla provides a `DynamicDbContext` that automatically builds EF model configurations for all discovered entities. You do **not** need to create a `DbContext` subclass.

The `DynamicDbContext`:

- Reads `[Table]`, `[Column]`, `[NotMapped]`, `[ForeignKey]`, `[JsonObject]` attributes from your entities
- Applies `IEntityTypeConfiguration<T>` from your assemblies
- Maps entities to tables using convention: entity name → table name in `dbo` schema

### Custom DbContext (optional)

If you need a custom `DbContext` for specific entities, use `[DbContextAttribute<T>]`:

```csharp
[DbContextAttribute<MyCustomDbContext>]
public class SpecialEntity : IEntity<int>, IEntityCreateable
{
    public int Id { get; set; }
    // ...
}
```

---

## `ReadRepository<TEntity, TContext>`

Implements `IReadRepository<TEntity, TKey>` with full EF Core support.

### Key behaviour

| Method | EF Core translation |
| ------ | ------------------- |
| `GetAll(filter)` | `DbSet.Where(...).Skip(...).Take(...).ToListAsync()` |
| `GetById(id)` | `DbSet.FindAsync(id)` |
| `GetByIds(ids)` | `DbSet.Where(x => ids.Contains(x.Id)).ToListAsync()` |
| `FirstOrDefault(filter)` | `DbSet.Where(...).FirstOrDefaultAsync()` |
| `GetCount(filter)` | `DbSet.Where(...).CountAsync()` |
| `GetSum(filter)` | `DbSet.Where(...).SumAsync(x => x.<Aggregate>)` |
| `Query` | `DbSet<TEntity>.AsNoTracking()` |
| `Where(expr)` | `DbSet.Where(expr)` |

### No-tracking reads

All read operations use `AsNoTracking()` by default for performance. Entities returned from read methods are **not** tracked by the change tracker.

If you need to update a returned entity, either:
- Re-attach it: `Context.Entry(entity).State = EntityState.Modified`
- Or re-fetch it through the update path (the update method handles this)

---

## `CreateRepository<TEntity, TContext>`

Implements `ICreateRepository<TEntity, TKey>`.

### Upsert

```csharp
// Insert if no match found, update if match exists
await repo.UpsertAsync(
    product,
    x => x.ExternalId);   // the property that identifies an existing record
```

The `matchExpression` lambda tells the upsert which property to use as the "natural key" for the lookup. If a record with the same value exists, it is updated; otherwise a new one is inserted.

### Merge

```csharp
// Like upsert but also deletes records no longer in the source collection
await repo.MergeAsync(
    incomingProduct,
    x => x.ExternalId);
```

---

## `RepositoryDependency`

A DI container object passed into all repository constructors internally. It holds the services a repository needs (event dispatcher, command dispatcher, service provider).

```csharp
public class RepositoryDependency(
    IServiceProvider resolver,
    IEventDispatcher events,
    ICommandDispatcher commands)
{
    public IServiceProvider Resolver { get; }
    public IEventDispatcher Events { get; }
    public ICommandDispatcher Commands { get; }
}
```

You do not interact with `RepositoryDependency` directly — it is wired automatically by the DI container.

---

## `EFDbTransaction`

The EF Core implementation of `IDbTransaction`, wrapping `IDbContextTransaction`.

```csharp
await using var tx = await repo.BeginTransaction();
try
{
    var order = await orderRepo.Create(newOrder);
    await lineRepo.Create(orderLines);
    await tx.Commit();
}
catch
{
    await tx.Rollback();
    throw;
}
```

---

## Advanced: Custom Repository Methods

For queries not covered by the standard interfaces, create a custom interface and implementation. Use `[Implement]` to register your custom class — the standard auto-registered repositories remain available alongside it.

```csharp
// Custom interface extending the auto-registered create repo
public interface IProductRepository : ICreateRepository<Product, int>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, bool inStockOnly = false);
    Task<decimal> GetAveragePriceAsync(int categoryId);
}

// Custom implementation — [Implement] registers IProductRepository only
[Implement(typeof(IProductRepository))]
public class ProductRepository :
    CreateRepository<Product, DynamicDbContext>,
    IProductRepository
{
    public ProductRepository(RepositoryDependency dep, DynamicDbContext ctx) : base(dep, ctx) { }

    public Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, bool inStockOnly = false)
    {
        var query = Query.Where(p => p.CategoryId == categoryId);
        if (inStockOnly)
            query = query.Where(p => p.Stock > 0);
        return Task.FromResult<IEnumerable<Product>>(query.ToList());
    }

    public async Task<decimal> GetAveragePriceAsync(int categoryId)
        => await Query
            .Where(p => p.CategoryId == categoryId)
            .AverageAsync(p => p.Price);
}
```

---

## Supported Databases

Any database provider supported by EF Core works:

| Database | NuGet |
| -------- | ----- |
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| MySQL | `Pomelo.EntityFrameworkCore.MySql` |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` |

---

## Filtering Translation

`IFilter` properties are translated by the EF repository as follows:

| Filter property | EF Core |
| --------------- | ------- |
| `Skip` + `Take` | `.Skip().Take()` |
| `OrderBy` + `Descending` | `.OrderBy()` / `.OrderByDescending()` using `System.Linq.Dynamic.Core` |
| `Search` | `.Where(e => <string props>.Contains(search))` |
| `Properties["X"].Equal` | `.Where(e => e.X == value)` |
| `Properties["X"].In` | `.Where(e => values.Contains(e.X))` |
| `Properties["X"].Between` | `.Where(e => e.X >= min && e.X <= max)` |
| `With[]` | `.Include(nav1).Include(nav2)` |

---

## See Also

- [Core Repository Interfaces](../core/repositories.md) — interface definitions
- [Filtering](../core/filtering.md) — `IFilter` parameters
- [Entities](../core/entities.md) — lifecycle interfaces and entity events

---

[Home](../../README.md) / [Docs](../index.md) / [Repositories](README.md) / **Entity Framework**
