# Entities

[Home](../../README.md) / [Docs](../index.md) / [Core](README.md) / Entities

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Entity/`

Entities are the domain objects of your application. Sencilla uses a set of interfaces to declare what an entity **is** and what operations it **supports**.

---

## `IEntity<TKey>` — Base Interface

Every entity must implement `IEntity<TKey>`, where `TKey` is the primary key type.

```csharp
public interface IEntity<TKey> : IBaseEntity
{
    TKey Id { get; set; }
}

// Non-generic marker (used internally for type checks)
public interface IBaseEntity { }

// Alias when key type doesn't matter at the call site
public interface IEntity : IEntity<int> { }
```

**Common key types:**

| Type | When to use |
| ---- | ----------- |
| `int` | Auto-increment integer (SQL IDENTITY) |
| `long` | Large auto-increment |
| `Guid` | Distributed systems, no coordination needed |
| `string` | Natural keys (e.g., ISO codes) |

---

## Lifecycle Marker Interfaces

These interfaces are **markers** — they have no methods. They signal to the framework (repositories, event system, validation) which operations are valid for a given entity.

### `IEntityCreateable`

Marks the entity as creatable. Adds a `CreatedDate` timestamp.

```csharp
public interface IEntityCreateable
{
    DateTime CreatedDate { get; set; }
}
```

When an entity implements this interface, the repository automatically sets `CreatedDate` before persisting.

---

### `IEntityUpdateable`

Marks the entity as updatable. Adds an `UpdatedDate` timestamp.

```csharp
public interface IEntityUpdateable
{
    DateTime UpdatedDate { get; set; }
}
```

---

### `IEntityDeleteable`

Marks the entity as hard-deletable. Enables `IDeleteRepository<TEntity, TKey>`.

```csharp
public interface IEntityDeleteable { }
```

Hard delete removes the row from the database permanently.

---

### `IEntityRemoveable`

Marks the entity as soft-deletable. Adds an `IsRemoved` flag. Enables `IRemoveRepository<TEntity, TKey>`.

```csharp
public interface IEntityRemoveable
{
    bool IsRemoved { get; set; }
}
```

Soft delete sets `IsRemoved = true`. The record remains in the database and can be restored.

---

### `IEntityHideable`

Marks the entity as hideable. Enables `IHideRepository<TEntity, TKey>`.

```csharp
public interface IEntityHideable
{
    bool IsHidden { get; set; }
}
```

Useful for temporarily making a record invisible without deleting it.

---

### `IEntityNameable`

Entities with a human-readable name.

```csharp
public interface IEntityNameable
{
    string Name { get; set; }
}
```

---

### `IEntityOrderable`

Entities that can be sorted by a user-defined order.

```csharp
public interface IEntityOrderable
{
    int Order { get; set; }
}
```

---

### `IEntityParentable`

Entities in a hierarchical structure (tree).

```csharp
public interface IEntityParentable<TKey>
{
    TKey? ParentId { get; set; }
}
```

---

### `IEntityPublishable`

Entities with a published/draft state.

```csharp
public interface IEntityPublishable
{
    bool IsPublished { get; set; }
}
```

---

## Entity Design Patterns

### Minimal entity (read-only lookup data)

```csharp
public class Country : IEntity<string>, IEntityNameable
{
    public string Id { get; set; } = string.Empty;   // ISO code e.g. "US"
    public string Name { get; set; } = string.Empty;
}
```

### Standard CRUD entity

```csharp
public class Article : IEntity<int>, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
```

### Soft-delete entity with visibility

```csharp
public class Product : IEntity<Guid>,
    IEntityCreateable,
    IEntityUpdateable,
    IEntityRemoveable,
    IEntityHideable,
    IEntityNameable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsRemoved { get; set; }
    public bool IsHidden { get; set; }
}
```

### Hierarchical entity

```csharp
public class Category : IEntity<int>,
    IEntityNameable,
    IEntityParentable<int>,
    IEntityOrderable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int Order { get; set; }
}
```

---

## Entity Events

When entities implement the appropriate lifecycle interfaces, the repository layer fires events automatically. You can subscribe to these events to add cross-cutting behaviour (audit logs, cache invalidation, notifications, etc.).

### Event Types

| Event | Fired when | Requires interface |
| ----- | ---------- | ------------------ |
| `EntityCreatingEvent<TEntity>` | Before create | `IEntityCreateable` |
| `EntityCreatedEvent<TEntity>` | After create | `IEntityCreateable` |
| `EntityUpdatingEvent<TEntity>` | Before update | `IEntityUpdateable` |
| `EntityUpdatedEvent<TEntity>` | After update | `IEntityUpdateable` |
| `EntityDeletingEvent<TEntity>` | Before delete | `IEntityDeleteable` |
| `EntityDeletedEvent<TEntity>` | After delete | `IEntityDeleteable` |
| `EntityReadingEvent<TEntity>` | On read | Any `IReadRepository` usage |

### `EntityBaseEvent<TEntity>`

All entity events inherit from `EntityBaseEvent<TEntity>`:

```csharp
public abstract class EntityBaseEvent<TEntity> : IEvent
    where TEntity : IBaseEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public string Type { get; init; }
    public TEntity Entity { get; init; }
}
```

### Handling Entity Events

```csharp
// Subscribe to post-create events for audit logging
[Implement]
public class ProductCreatedHandler : IEventHandler<EntityCreatedEvent<Product>>
{
    private readonly IAuditLog _audit;

    public ProductCreatedHandler(IAuditLog audit) => _audit = audit;

    public Task HandleAsync(EntityCreatedEvent<Product> @event, CancellationToken token)
        => _audit.LogAsync($"Product {event.Entity.Id} created", token);
}
```

---

## Interface Combination Reference

| Scenario | Interfaces to implement |
| -------- | ----------------------- |
| Read-only lookup | `IEntity<TKey>` |
| Standard CRUD | `IEntity<TKey>`, `IEntityCreateable`, `IEntityUpdateable`, `IEntityDeleteable` |
| Soft-delete record | `IEntity<TKey>`, `IEntityCreateable`, `IEntityUpdateable`, `IEntityRemoveable` |
| Publishable content | Add `IEntityPublishable` |
| Menu / sortable list | Add `IEntityOrderable` |
| Tree/hierarchy | Add `IEntityParentable<TKey>` |
| Everything | All of the above |

---

## See Also

- [Repository Interfaces](repositories.md) — how entities flow through CRUD operations
- [Filtering](filtering.md) — query entities with `IFilter`
- [Entity Framework Repository](../repositories/entity-framework.md) — EF Core implementation details

---

[Home](../../README.md) / [Docs](../index.md) / [Core](README.md) / **Entities**
