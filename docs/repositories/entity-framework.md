# Entity Framework Core Repository

**NuGet:** `Sencilla.Repository.EntityFramework`
**Namespace:** `Sencilla.Repository.EntityFramework`
**Source:** `libs/repositories/EntityFramework/`

**Dependencies:** `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `System.Linq.Dynamic.Core`, `Sencilla.Core`

The EF Core repository is the **primary data access implementation**. It wraps a `DbContext` and provides the full Sencilla repository interface hierarchy.

---

## Installation

```bash
dotnet add package Sencilla.Repository.EntityFramework
```

---

## Class Hierarchy

```text
Resolveable
└── BaseRepository<TContext>                      Holds DbContext + RepositoryDependency
    ├── ReadRepository<TEntity, TContext>          IReadRepository<TEntity, TKey>
    │   ├── CreateRepository<TEntity, TContext>    + ICreateRepository
    │   ├── UpdateRepository<TEntity, TContext>    + IUpdateRepository
    │   ├── DeleteRepository<TEntity, TContext>    + IDeleteRepository
    │   ├── RemoveRepository<TEntity, TContext>    + IRemoveRepository
    │   └── HideRepository<TEntity, TContext>      + IHideRepository
    └── RepositoryDependency<TContext>             DI container for repository dependencies
```

> **How to read this:** `ReadRepository` gives you all read operations. Inherit from it and add `I*Repository` interfaces to enable write operations. The EF implementations are discovered automatically and mixed in.

---

## Quick Setup

### 1. Define the entity

```csharp
public class Product : IEntity<int>, IEntityCreatable, IEntityUpdatable, IEntityDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 2. Add to DbContext

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}
```

### 3. Implement the repository

```csharp
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

That's it. The framework provides the full implementation for all declared interfaces. You don't override anything unless you need custom behaviour.

### 4. Register

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSencilla(typeof(Program).Assembly);
```

---

## `BaseRepository<TContext>`

All repositories inherit from `BaseRepository<TContext>`, which provides:

```csharp
public abstract class BaseRepository<TContext> : Resolveable
    where TContext : DbContext
{
    protected TContext Context { get; }
    protected RepositoryDependency<TContext> Dependency { get; }

    public Task<int> Save(CancellationToken token = default)
        => Context.SaveChangesAsync(token);
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
    x => x.ExternalId,   // the property that identifies an existing record
    "Category");          // navigation properties to reload after upsert
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

## `RepositoryDependency<TContext>`

A DI container object passed into all repository constructors. It holds all the services a repository might need (DbContext, event dispatcher, etc.) without requiring each class to declare a large constructor.

```csharp
public class RepositoryDependency<TContext> where TContext : DbContext
{
    public TContext Context { get; }
    public IEventDispatcher Events { get; }
    public IServiceProvider Services { get; }
    // ...
}
```

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

When the standard interface isn't enough, add methods to your own interface and implement them using `Query` or `Context`:

```csharp
public interface IProductRepository : ICreateRepository<Product, int>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, bool inStockOnly = false);
    Task<decimal> GetAveragePriceAsync(int categoryId);
}

[Implement]
public class ProductRepository :
    ReadRepository<Product, AppDbContext>,
    IProductRepository
{
    public ProductRepository(RepositoryDependency<AppDbContext> dep) : base(dep) { }

    public Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, bool inStockOnly = false)
    {
        var query = Query.Where(p => p.CategoryId == categoryId);
        if (inStockOnly)
            query = query.Where(p => p.Stock > 0);
        return Task.FromResult<IEnumerable<Product>>(query.ToList());
    }

    public Task<decimal> GetAveragePriceAsync(int categoryId)
        => Context.Products
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
