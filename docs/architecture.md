# Architecture Overview

[Home](../README.md) / [Docs](index.md) / Architecture

Sencilla is a **layered, modular framework**. The modules are independent NuGet packages with explicit dependency relationships. You add only what you need.

## Design Philosophy

- **Interfaces first** — All public contracts are interfaces. Implementations are swappable.
- **Convention over configuration** — Attributes like `[Implement]`, `[CrudApi]`, `[ScheduleTask]` eliminate boilerplate registration.
- **Provider pattern** — Messaging, file storage, and scheduling use provider-agnostic interfaces with pluggable backends.
- **No magic at runtime** — Source generators produce code at compile time; no reflection-heavy startup.
- **Nullable by default** — All projects enable `<Nullable>enable</Nullable>`.

---

## Module Dependency Graph

```text
                         ┌─────────────────────┐
                         │    Sencilla.Core     │  ← Start here. Every package
                         │  (Foundation Layer)  │    depends on this.
                         └──────────┬──────────┘
                                    │
              ┌─────────────────────┼─────────────────────┐
              │                     │                     │
   ┌──────────▼──────────┐ ┌────────▼────────┐ ┌─────────▼────────┐
   │    Sencilla.Web     │ │Sencilla.Messaging│ │Sencilla.Scheduler│
   │  (Web Layer)        │ │   (Core only)    │ │  (Core + Cronos) │
   └──────────┬──────────┘ └────────┬────────┘ └─────────┬────────┘
              │                     │                     │
   ┌──────────▼──────────┐ ┌────────▼────────────────────▼────────┐
   │  Components, Files  │ │  Providers: RabbitMQ, Kafka, Redis,  │
   │  (Users, Auth, i18n)│ │  ServiceBus, SignalR, InMemoryQueue  │
   └─────────────────────┘ └──────────────────────────────────────┘

   ┌─────────────────────────────────────────────────────────────────┐
   │  Repository Implementations (all depend on Sencilla.Core)       │
   │  EntityFramework · HttpClient · SqlMapper                       │
   └─────────────────────────────────────────────────────────────────┘

   ┌─────────────────────────────────────────────────────────────────┐
   │  Sencilla.Extensions.EntityFrameworkCore                         │
   │  (standalone EF Core utilities, depends on Sencilla.Core)       │
   └─────────────────────────────────────────────────────────────────┘
```

---

## Package Inventory

### Foundation

| Package | Namespace | Key Responsibility |
| ------- | --------- | ------------------ |
| `Sencilla.Core` | `Sencilla.Core` | Entities, repository interfaces, IFilter, CQRS, DI discovery |

### Web Layer

| Package | Namespace | Key Responsibility |
| ------- | --------- | ------------------ |
| `Sencilla.Web` | `Sencilla.Web` | Base API controller, CRUD auto-generation, model binding |
| `Sencilla.Web.MinimalApi` | `Sencilla.Web.MinimalApi` | `IEndpoint` for minimal API pattern |

### Data Access

| Package | Namespace | Key Responsibility |
| ------- | --------- | ------------------ |
| `Sencilla.Repository.EntityFramework` | `Sencilla.Repository.EntityFramework` | Full EF Core repository, transactions, upsert, merge |
| `Sencilla.Repository.HttpClient` | `Sencilla.Repository.HttpClient` | Calls remote REST APIs via repository interface |
| `Sencilla.Repository.SqlMapper` | `Sencilla.Repository.SqlMapper` | Raw SQL + expression query builder |
| `Sencilla.Extensions.EntityFrameworkCore` | — | EF Core utilities and extensions |

### Messaging

| Package | Provider | Notes |
| ------- | -------- | ----- |
| `Sencilla.Messaging` | — | Provider-agnostic core, `IMessageDispatcher`, `IMessageHandler<T>` |
| `Sencilla.Messaging.InMemoryQueue` | In-process | Use for dev, testing, single-instance apps |
| `Sencilla.Messaging.RabbitMQ` | RabbitMQ 7.x | Recommended for microservices |
| `Sencilla.Messaging.Kafka` | Confluent Kafka | High-throughput event streaming |
| `Sencilla.Messaging.ServiceBus` | Azure Service Bus | Cloud-native, managed |
| `Sencilla.Messaging.Redis` | Redis | Lightweight pub/sub |
| `Sencilla.Messaging.SignalR` | ASP.NET SignalR | Real-time WebSocket push |
| `Sencilla.Messaging.Mediator` | MediatR | In-process mediator |
| `Sencilla.Messaging.Scheduler` | Scheduler | Dispatch messages on a schedule |
| `Sencilla.Messaging.EntityFramework` | EF Core | Persisted message outbox |
| `Sencilla.Messaging.SourceGenerator` | — | Compile-time code generation |

### Scheduling

| Package | Namespace | Key Responsibility |
| ------- | --------- | ------------------ |
| `Sencilla.Scheduler` | `Sencilla.Scheduler` | Cron scheduler, `IScheduledTaskHandler`, `IScheduledTasksScheduler` |
| `Sencilla.Scheduler.EntityFramework` | — | Database persistence for tasks |
| `Sencilla.Scheduler.SourceGenerator` | — | Compile-time task registration |

### Components

| Package | Domain | Key Responsibility |
| ------- | ------ | ------------------ |
| `Sencilla.Component.Users` | Identity | User entity, profile management |
| `Sencilla.Component.Auth` | Identity | JWT, OAuth, authentication flows |
| `Sencilla.Component.Security` | Identity | Roles, permissions, authorization |
| `Sencilla.Component.Config` | Infrastructure | Runtime application configuration |
| `Sencilla.Component.I18n` | Localization | Translations, locale management |
| `Sencilla.Component.Geography` | Domain | Country/region/location data |
| `Sencilla.Component.Validation` | Cross-cutting | Validation rules and pipelines |

### File Storage

| Package | Backend | Notes |
| ------- | ------- | ----- |
| `Sencilla.Component.Files` | — | Core interface, `IFileStorage` |
| `Sencilla.Component.Files.LocalDrive` | File system | Dev and on-premise use |
| `Sencilla.Component.Files.AzureStorage` | Azure Blob Storage | Azure SDK 12.x |
| `Sencilla.Component.Files.AmazonS3` | AWS S3 | AWS SDK 4.x |
| `Sencilla.Component.Files.GoogleStorage` | GCS | Google Cloud SDK |
| `Sencilla.Component.Files.Database` | EF Core | Store files in the database |

### Mappers

| Package | Purpose |
| ------- | ------- |
| `Sencilla.Mapper.Sql` | Expression-based SQL query builder with multi-DB support |
| `Sencilla.Mapper.Entity` | Entity-to-entity mapping |
| `Sencilla.Mapper.Excel` | Excel import/export |

---

## Typical Application Stack

A typical web API application uses this combination:

```text
Your Application
    │
    ├── Sencilla.Core               (always)
    ├── Sencilla.Web                (if using MVC controllers)
    ├── Sencilla.Web.MinimalApi     (if using minimal APIs)
    ├── Sencilla.Repository.EntityFramework  (primary data access)
    ├── Sencilla.Messaging          (if event-driven)
    │   └── Sencilla.Messaging.RabbitMQ  (chosen provider)
    ├── Sencilla.Scheduler          (if background jobs needed)
    ├── Sencilla.Component.Users    (if multi-user system)
    └── Sencilla.Component.Files    (if file uploads needed)
        └── Sencilla.Component.Files.AzureStorage
```

---

## Cross-Cutting Concepts

### Auto-Discovery DI

All Sencilla packages register themselves via `[AutoDiscovery]` assemblies and `[Implement]` attributes. You don't manually register framework types. One call wires everything:

```csharp
builder.Services.AddSencilla(builder.Configuration);
```

### Entity Lifecycle

Entities declare what operations they support through marker interfaces:

```text
IEntity<TKey>
├── IEntityCreateable    → Has CreatedDate timestamp, fires EntityCreatingEvent / EntityCreatedEvent
├── IEntityUpdateable    → Has UpdatedDate timestamp, fires EntityUpdatingEvent / EntityUpdatedEvent
├── IEntityDeleteable    → Hard delete, fires EntityDeletingEvent / EntityDeletedEvent
├── IEntityRemoveable    → Soft delete (IsRemoved flag)
├── IEntityHideable      → Visibility toggle (IsHidden flag)
├── IEntityNameable      → Has a Name property
├── IEntityOrderable     → Has an Order/Sort property
├── IEntityParentable    → Hierarchical (ParentId)
└── IEntityPublishable   → Publishable state (IsPublished)
```

### Repository Hierarchy

Repositories are composed from fine-grained interfaces:

```text
IBaseRepository
└── IReadRepository<TEntity, TKey>
    ├── ICreateRepository<TEntity, TKey>
    ├── IUpdateRepository<TEntity, TKey>
    ├── IDeleteRepository<TEntity, TKey>
    ├── IRemoveRepository<TEntity, TKey>
    └── IHideRepository<TEntity, TKey>
```

Inject only the interface you need — `IReadRepository` for read-only services, `ICreateRepository` where you only create, etc.

---

## Source Repository Layout

```text
libs/
├── core/                      → Sencilla.Core
├── web/                       → Sencilla.Web
├── webapi/
│   ├── ApiControllers/        → Sencilla.Web.ApiControllers
│   └── MinimalApi/            → Sencilla.Web.MinimalApi
├── repositories/
│   ├── EntityFramework/       → Sencilla.Repository.EntityFramework
│   ├── SqlMapper/             → Sencilla.Repository.SqlMapper
│   └── HttpClient/            → Sencilla.Repository.HttpClient
├── messaging/
│   ├── Core/                  → Sencilla.Messaging
│   ├── RabbitMQ/              → Sencilla.Messaging.RabbitMQ
│   ├── Kafka/                 → Sencilla.Messaging.Kafka
│   ├── ServiceBus/            → Sencilla.Messaging.ServiceBus
│   ├── Redis/                 → Sencilla.Messaging.Redis
│   ├── SignalR/               → Sencilla.Messaging.SignalR
│   ├── InMemoryQueue/         → Sencilla.Messaging.InMemoryQueue
│   ├── Mediator/              → Sencilla.Messaging.Mediator
│   ├── Scheduler/             → Sencilla.Messaging.Scheduler
│   ├── EntityFramework/       → Sencilla.Messaging.EntityFramework
│   └── SourceGenerator/       → Sencilla.Messaging.SourceGenerator
├── scheduler/
│   ├── Core/                  → Sencilla.Scheduler
│   ├── EntityFramework/       → Sencilla.Scheduler.EntityFramework
│   └── SourceGenerator/       → Sencilla.Scheduler.SourceGenerator
├── components/
│   ├── Users/                 → Sencilla.Component.Users
│   ├── Auth/                  → Sencilla.Component.Auth
│   ├── Security/              → Sencilla.Component.Security
│   ├── Config/                → Sencilla.Component.Config
│   ├── I18n/                  → Sencilla.Component.I18n
│   ├── Geography/             → Sencilla.Component.Geography
│   └── Validation/            → Sencilla.Component.Validation
├── files/
│   ├── Core/                  → Sencilla.Component.Files
│   ├── LocalDrive/            → Sencilla.Component.Files.LocalDrive
│   ├── AzureStorage/          → Sencilla.Component.Files.AzureStorage
│   ├── AmazonS3/              → Sencilla.Component.Files.AmazonS3
│   ├── GoogleStorage/         → Sencilla.Component.Files.GoogleStorage
│   └── Database/              → Sencilla.Component.Files.Database
├── mappers/
│   ├── Sql/                   → Sencilla.Mapper.Sql
│   ├── Entity/                → Sencilla.Mapper.Entity
│   └── Excel/                 → Sencilla.Mapper.Excel
└── extensions/
    └── EntityFrameworkCore/   → Sencilla.Extensions.EntityFrameworkCore
```

---

[Home](../README.md) / [Docs](index.md) / **Architecture**
