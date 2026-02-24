# Sencilla Framework — Documentation

Welcome to the Sencilla Framework documentation. This guide is structured to take you from first install through advanced architectural patterns.

## Navigation

| Section | What you'll learn |
| ------- | ----------------- |
| [Getting Started](getting-started.md) | Installation, project setup, first entity and repository |
| [Architecture](architecture.md) | Module overview, dependency graph, design philosophy |
| **Core Module** | |
| [Core Overview](core/README.md) | What lives in `Sencilla.Core` and why |
| [Entities](core/entities.md) | `IEntity`, lifecycle interfaces, entity events |
| [Repository Interfaces](core/repositories.md) | `IReadRepository`, `ICreateRepository`, and the full hierarchy |
| [Filtering](core/filtering.md) | `IFilter` — pagination, sorting, search, property filters |
| [Commands & Events](core/commands-events.md) | CQRS pattern — commands, events, dispatchers, handlers |
| [Dependency Injection](core/dependency-injection.md) | Auto-discovery, `[Implement]`, lifetimes, `StarterKit` |
| [Exceptions](core/exceptions.md) | HTTP-mapped exception hierarchy |
| **Repository Implementations** | |
| [Implementations Overview](repositories/README.md) | Which implementation to choose and when |
| [Entity Framework Core](repositories/entity-framework.md) | `BaseRepository`, `ReadRepository`, transactions |
| [HTTP Client](repositories/http-client.md) | Remote API repository pattern |
| [SQL Mapper](repositories/sql-mapper.md) | Raw SQL with expression-based query building |
| **Other Modules** | |
| [Messaging](messaging/README.md) | Provider-agnostic message bus, routing, middleware |
| [Scheduler](scheduler/README.md) | Cron-based task scheduling |
| [Web](web/README.md) | CRUD API auto-generation, model binding |
| [Web API (Minimal API)](webapi/README.md) | `IEndpoint`, minimal API pattern |
| [Components](components/README.md) | Users, Auth, Security, i18n, Geography |
| [Files](files/README.md) | File storage abstraction and providers |
| [Mappers](mappers/README.md) | SQL mapper, Entity mapper, Excel export |

---

## AI Coding Reference

If you are an AI assistant helping a developer build on top of Sencilla, start here:

1. **Every project needs `Sencilla.Core`** — it defines all foundational interfaces
2. **Entities** implement `IEntity<TKey>` plus lifecycle marker interfaces (`IEntityCreateable`, `IEntityUpdateable`, etc.)
3. **Repositories** are injected by interface — `IReadRepository<TEntity, TKey>`, `ICreateRepository<TEntity, TKey>`, etc.
4. **Filtering** is done via `IFilter` — pass it to `GetAll()`, `GetCount()`, etc.
5. **DI** is attribute-driven — add `[Implement]` to a class and it self-registers
6. **Messaging** is provider-swappable at registration time — the same handler code works with any broker

Read [Architecture](architecture.md) first for the big picture, then the relevant module doc.
