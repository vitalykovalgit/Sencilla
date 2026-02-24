# Repository Interfaces

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Repository/`

Sencilla uses a **composable repository hierarchy**. Each interface represents a single capability. Your concrete repository implements the interfaces that match the entity's lifecycle markers.

---

## Interface Hierarchy

```text
IBaseRepository
└── IReadRepository<TEntity, TKey>
    ├── ICreateRepository<TEntity, TKey>
    ├── IUpdateRepository<TEntity, TKey>
    ├── IDeleteRepository<TEntity, TKey>   (hard delete)
    ├── IRemoveRepository<TEntity, TKey>   (soft delete)
    └── IHideRepository<TEntity, TKey>     (hide/show)
```

---

## `IBaseRepository`

The root interface. Contains only `Save`.

```csharp
public interface IBaseRepository
{
    Task<int> Save(CancellationToken token = default);
}
```

Normally you don't inject `IBaseRepository` directly; it is exposed by all higher-level interfaces.

---

## `IReadRepository<TEntity, TKey>`

Provides all **read** operations. Inject this when a consumer must only read data.

```csharp
public interface IReadRepository<TEntity, TKey> : IBaseRepository
    where TEntity : IEntity<TKey>
{
    // Raw LINQ queryable — use for complex queries not covered by GetAll
    IQueryable<TEntity> Query { get; }

    // Fetch single entity by primary key
    Task<TEntity?> GetById(TKey id, params string[] with);

    // Fetch multiple entities by primary key list
    Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, params string[] with);

    // Fetch all entities, optionally filtered
    Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, params string[] with);

    // Fetch first match or null
    Task<TEntity?> FirstOrDefault(IFilter? filter = null, params string[] with);

    // Count matching entities
    Task<int> GetCount(IFilter? filter = null);

    // Aggregations
    Task<object> GetSum(IFilter? filter = null);
    Task<object> GetMax(IFilter? filter = null);
    Task<object> GetMin(IFilter? filter = null);
    Task<double> GetAverage(IFilter? filter = null);

    // Start a database transaction
    Task<IDbTransaction> BeginTransaction();

    // Build a filtered queryable from an expression
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
}
```

### Parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `filter` | `IFilter?` | Pagination, sorting, search, property filters. `null` means no filtering. |
| `with` | `string[]` | Navigation property names to eager-load (equivalent to `.Include()`). |
| `id` | `TKey` | Primary key value. |
| `ids` | `IEnumerable<TKey>` | Multiple primary keys for batch fetch. |

### Examples

```csharp
// All products, first page
var products = await repo.GetAll(new Filter { Skip = 0, Take = 20 });

// Single product with its category
var product = await repo.GetById(productId, "Category");

// Count active products
int count = await repo.GetCount(new Filter { /* active = true filter */ });

// Raw LINQ for complex join
var result = repo.Query
    .Where(p => p.Price > 100)
    .OrderByDescending(p => p.CreatedAt)
    .Take(5)
    .ToList();
```

---

## `ICreateRepository<TEntity, TKey>`

Adds **create** (insert) operations.

```csharp
public interface ICreateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
{
    // Insert a single entity
    Task<TEntity?> Create(TEntity entity, CancellationToken token = default);

    // Insert multiple entities (params overload)
    Task<IEnumerable<TEntity>> Create(params TEntity[] entities);

    // Insert multiple entities with cancellation
    Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default);

    // Insert or update based on a match expression
    // matchExpression — expression that identifies the existing record to match (e.g., x => x.ExternalId)
    Task<TEntity> UpsertAsync(TEntity entity, Expression<Func<TEntity, object>> matchExpression, params string[] with);
    Task<IEnumerable<TEntity>> UpsertAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> matchExpression, params string[] with);

    // Three-way merge — insert/update/delete to bring DB in sync with the provided collection
    Task<TEntity> MergeAsync(TEntity entity, Expression<Func<TEntity, object>> matchExpression, params string[] with);
}
```

### Examples

```csharp
// Create one
var created = await repo.Create(new Product { Name = "Widget", Price = 9.99m });

// Create many
var bulk = await repo.Create(productList, cancellationToken);

// Upsert by external ID (insert if not found, update if found)
await repo.UpsertAsync(
    incoming,
    x => x.ExternalId,
    "Category");
```

---

## `IUpdateRepository<TEntity, TKey>`

Adds **update** operations.

```csharp
public interface IUpdateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
{
    Task<TEntity?> Update(TEntity entity, CancellationToken token = default);
    Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default);
}
```

### Example

```csharp
product.Price = 12.99m;
await repo.Update(product);
```

---

## `IDeleteRepository<TEntity, TKey>`

Adds **hard delete** (permanent removal).

```csharp
public interface IDeleteRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
{
    // Delete by primary key
    Task<bool> Delete(TKey id, CancellationToken token = default);

    // Delete a collection
    Task<int> Delete(IEnumerable<TEntity> entities, CancellationToken token = default);
}
```

> Use hard delete only when you genuinely want the record gone. For auditability, prefer `IRemoveRepository` (soft delete).

---

## `IRemoveRepository<TEntity, TKey>`

Adds **soft delete** (sets `IsRemoved = true`). Entity must implement `IEntityRemoveable`.

```csharp
public interface IRemoveRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>, IEntityRemoveable
{
    Task<bool> Remove(TKey id, CancellationToken token = default);
    Task<int> Remove(IEnumerable<TEntity> entities, CancellationToken token = default);
}
```

---

## `IHideRepository<TEntity, TKey>`

Toggles `IsHidden` flag. Entity must implement `IEntityHideable`.

```csharp
public interface IHideRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>, IEntityHideable
{
    Task<bool> Hide(TKey id, CancellationToken token = default);
    Task<int> Hide(IEnumerable<TEntity> entities, CancellationToken token = default);
}
```

---

## `IDbTransaction`

Returned by `BeginTransaction()`. Use for unit-of-work across multiple repository calls.

```csharp
public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    Task Commit(CancellationToken token = default);
    Task Rollback(CancellationToken token = default);
}
```

### Usage

```csharp
await using var tx = await orderRepo.BeginTransaction();
try
{
    var order = await orderRepo.Create(newOrder);
    await inventoryRepo.Update(updatedItems);
    await tx.Commit();
}
catch
{
    await tx.Rollback();
    throw;
}
```

---

## Interface Selection Guide

Choose the **narrowest** interface that covers your use case. This makes intent clear and prevents misuse.

| Scenario | Inject |
| -------- | ------ |
| Dashboard / reporting (read only) | `IReadRepository<TEntity, TKey>` |
| Import job (creates records) | `ICreateRepository<TEntity, TKey>` |
| Profile editing (update only) | `IUpdateRepository<TEntity, TKey>` |
| Admin deletion | `IDeleteRepository<TEntity, TKey>` |
| Archive/restore | `IRemoveRepository<TEntity, TKey>` |
| Full CRUD controller | All of the above (inject separately or implement all on one concrete class) |

---

## Implementing a Repository

See [Entity Framework Repository](../repositories/entity-framework.md) for the concrete implementation guide. The pattern is:

```csharp
[Implement]
public class ProductRepository :
    ReadRepository<Product, AppDbContext>,       // provides IReadRepository
    ICreateRepository<Product, int>,             // adds Create/Upsert/Merge
    IUpdateRepository<Product, int>,             // adds Update
    IDeleteRepository<Product, int>              // adds Delete
{
    public ProductRepository(RepositoryDependency<AppDbContext> dep) : base(dep) { }
}
```

---

## See Also

- [Entities](entities.md) — entity interfaces and lifecycle markers
- [Filtering](filtering.md) — `IFilter` passed to `GetAll`, `GetCount`, etc.
- [Entity Framework Repository](../repositories/entity-framework.md) — concrete EF Core implementation
- [HTTP Client Repository](../repositories/http-client.md) — REST API backend
