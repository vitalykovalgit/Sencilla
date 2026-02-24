# Repository Implementations

Sencilla provides three concrete repository backends. All implement the same `IReadRepository` / `ICreateRepository` / etc. interfaces defined in `Sencilla.Core`, so switching backends is a one-line change in your DI registration.

---

## Choosing a Backend

| Backend | Package | When to use |
| ------- | ------- | ----------- |
| [Entity Framework Core](entity-framework.md) | `Sencilla.Repository.EntityFramework` | Primary choice for relational databases (SQL Server, PostgreSQL, MySQL, SQLite). Full EF Core support. |
| [HTTP Client](http-client.md) | `Sencilla.Repository.HttpClient` | Your data lives behind a REST API. Wrap a remote service with the same repository interface. |
| [SQL Mapper](sql-mapper.md) | `Sencilla.Repository.SqlMapper` | Raw SQL with expression-based query building. Use when EF Core is too heavy or you need fine-grained SQL control. |

---

## Shared Interface Contract

All backends implement the interfaces from `Sencilla.Core`. Your business logic only depends on the interfaces, never on the implementation:

```csharp
// ✅ Correct — depends on interface (Core)
public class OrderService(IReadRepository<Order, int> orders) { ... }

// ❌ Wrong — depends on implementation
public class OrderService(ReadRepository<Order, AppDbContext> orders) { ... }
```

This means you can:
- Unit test with a mock `IReadRepository<T, TKey>`
- Swap from EF Core to HTTP Client without changing service code
- Use different backends for different entities in the same app

---

## See Also

- [Core Repository Interfaces](../core/repositories.md) — full interface API reference
- [Entity Framework Repository](entity-framework.md)
- [HTTP Client Repository](http-client.md)
- [SQL Mapper Repository](sql-mapper.md)
