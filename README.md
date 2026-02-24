# Sencilla Framework

[![NuGet](https://img.shields.io/nuget/v/Sencilla.Core?label=Sencilla.Core&color=004880)](https://www.nuget.org/packages/Sencilla.Core)
[![Build](https://github.com/vitalykovalgit/Sencilla/actions/workflows/build.yml/badge.svg)](https://github.com/vitalykovalgit/Sencilla/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> A comprehensive, modular .NET 10 framework that provides battle-tested patterns — repository, CQRS, messaging, scheduling, and more — so you can focus on your business logic.

---

## What is Sencilla?

Sencilla is a **modular application framework** for .NET 10 that standardizes the way you build web applications. Instead of wiring up the same patterns project after project, Sencilla gives you:

- **Repository pattern** with a clean interface hierarchy — read, create, update, delete, soft-delete, hide
- **CQRS** — commands, queries, and events with built-in dispatchers and middleware
- **Messaging** — provider-agnostic message bus (RabbitMQ, Kafka, Azure Service Bus, Redis, SignalR, and more)
- **Scheduling** — cron-based task scheduling with EF Core persistence
- **Auto-discovery DI** — attribute-based service registration, zero boilerplate
- **Web scaffolding** — CRUD API controllers generated from a single attribute
- **Pre-built components** — Users, Auth, Security, i18n, Geography, Files (S3, Azure, GCS, local)

All modules are independent NuGet packages. Use only what you need.

---

## Quick Start

### 1. Install the core package

```bash
dotnet add package Sencilla.Core
```

### 2. Define your entity

```csharp
using Sencilla.Core;

public class Product : IEntity<int>, IEntityCreatable, IEntityUpdatable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 3. Add Entity Framework repository

```bash
dotnet add package Sencilla.Repository.EntityFramework
```

```csharp
using Sencilla.Core;
using Sencilla.Repository.EntityFramework;

// Your EF Core DbContext
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
}

// Implement the repository
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

### 4. Register services

```csharp
// Program.cs
builder.Services.AddSencilla(typeof(Program).Assembly);
```

### 5. Inject and use

```csharp
app.MapGet("/products", async (IReadRepository<Product, int> repo) =>
    await repo.GetAll());

app.MapPost("/products", async (Product product, ICreateRepository<Product, int> repo) =>
    await repo.Create(product));
```

---

## Packages

| Package | Description | NuGet |
| ------- | ----------- | ----- |
| `Sencilla.Core` | Foundation — entities, interfaces, DI, CQRS | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Core)](https://www.nuget.org/packages/Sencilla.Core) |
| `Sencilla.Web` | ASP.NET Core web infrastructure, CRUD controllers | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Web)](https://www.nuget.org/packages/Sencilla.Web) |
| `Sencilla.Web.MinimalApi` | Minimal API endpoint abstractions | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Web.MinimalApi)](https://www.nuget.org/packages/Sencilla.Web.MinimalApi) |
| `Sencilla.Repository.EntityFramework` | EF Core repository implementation | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Repository.EntityFramework)](https://www.nuget.org/packages/Sencilla.Repository.EntityFramework) |
| `Sencilla.Repository.HttpClient` | Remote API repository via HttpClient | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Repository.HttpClient)](https://www.nuget.org/packages/Sencilla.Repository.HttpClient) |
| `Sencilla.Messaging` | Provider-agnostic message bus core | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Messaging)](https://www.nuget.org/packages/Sencilla.Messaging) |
| `Sencilla.Messaging.RabbitMQ` | RabbitMQ provider | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Messaging.RabbitMQ)](https://www.nuget.org/packages/Sencilla.Messaging.RabbitMQ) |
| `Sencilla.Messaging.Kafka` | Kafka provider | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Messaging.Kafka)](https://www.nuget.org/packages/Sencilla.Messaging.Kafka) |
| `Sencilla.Messaging.ServiceBus` | Azure Service Bus provider | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Messaging.ServiceBus)](https://www.nuget.org/packages/Sencilla.Messaging.ServiceBus) |
| `Sencilla.Messaging.InMemoryQueue` | In-process message queue (dev/test) | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Messaging.InMemoryQueue)](https://www.nuget.org/packages/Sencilla.Messaging.InMemoryQueue) |
| `Sencilla.Scheduler` | Cron-based task scheduler | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Scheduler)](https://www.nuget.org/packages/Sencilla.Scheduler) |
| `Sencilla.Component.Users` | User management | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Component.Users)](https://www.nuget.org/packages/Sencilla.Component.Users) |
| `Sencilla.Component.Security` | Authorization & roles | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Component.Security)](https://www.nuget.org/packages/Sencilla.Component.Security) |
| `Sencilla.Component.Files` | File storage abstraction | [![NuGet](https://img.shields.io/nuget/v/Sencilla.Component.Files)](https://www.nuget.org/packages/Sencilla.Component.Files) |

---

## Documentation

Full documentation lives in the [`docs/`](docs/) folder.

| Section | Description |
| ------- | ----------- |
| [Getting Started](docs/getting-started.md) | Installation, first steps, and project setup |
| [Architecture](docs/architecture.md) | Module dependency graph and design decisions |
| [Core](docs/core/README.md) | Entities, repositories, CQRS, DI, exceptions |
| [Repository Implementations](docs/repositories/README.md) | EF Core, HttpClient, SqlMapper |
| [Messaging](docs/messaging/README.md) | Message bus, providers, routing |
| [Scheduler](docs/scheduler/README.md) | Scheduled tasks, cron expressions |
| [Web](docs/web/README.md) | CRUD controllers, model binding |
| [Web API](docs/webapi/README.md) | Minimal API endpoints |
| [Components](docs/components/README.md) | Users, Auth, Security, i18n, Geography |
| [Files](docs/files/README.md) | File storage providers |
| [Mappers](docs/mappers/README.md) | SQL mapper, Entity mapper, Excel |

---

## Repository Structure

```text
Sencilla/
├── libs/                          # All library source code
│   ├── core/                      # Foundation — must-have for every project
│   ├── web/                       # ASP.NET Core integration
│   ├── webapi/                    # Web API patterns (controllers + minimal API)
│   ├── messaging/                 # Event-driven messaging
│   │   ├── Core/                  # Provider-agnostic interfaces
│   │   ├── RabbitMQ/              # RabbitMQ provider
│   │   ├── Kafka/                 # Kafka provider
│   │   ├── ServiceBus/            # Azure Service Bus provider
│   │   ├── Redis/                 # Redis provider
│   │   ├── SignalR/               # SignalR (WebSocket) provider
│   │   ├── InMemoryQueue/         # In-process queue (dev/test)
│   │   ├── Mediator/              # MediatR integration
│   │   ├── Scheduler/             # Scheduled message dispatch
│   │   └── SourceGenerator/       # Code generation
│   ├── scheduler/                 # Cron task scheduler
│   ├── repositories/              # Data access implementations
│   │   ├── EntityFramework/       # EF Core repository
│   │   ├── SqlMapper/             # Raw SQL repository
│   │   └── HttpClient/            # Remote API repository
│   ├── components/                # Pre-built domain components
│   │   ├── Users/
│   │   ├── Auth/
│   │   ├── Security/
│   │   ├── Config/
│   │   ├── I18n/
│   │   ├── Geography/
│   │   └── Validation/
│   ├── files/                     # File storage
│   │   ├── Core/
│   │   ├── LocalDrive/
│   │   ├── AzureStorage/
│   │   ├── AmazonS3/
│   │   ├── GoogleStorage/
│   │   └── Database/
│   ├── mappers/                   # Data mapping utilities
│   └── extensions/                # EF Core extensions
├── tests/                         # Test projects
├── docs/                          # Documentation
├── builds/                        # Build scripts
│   ├── github/                    # GitHub Actions workflows
│   └── local/                     # Local development scripts
└── .github/
    └── workflows/                 # Active GitHub Actions
```

---

## Local Development

To build and test NuGet packages locally:

```powershell
# PowerShell
.\builds\local\create-local-packages.ps1 -Version "10.0.0-dev.1"

# macOS/Linux
./builds/local/create-local-packages.sh --version "10.0.0-dev.1"
```

Packages are output to `~/local-nuget`. See [Local Development Guide](builds/local/README.md) for setup.

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes
4. Push to the branch and open a Pull Request

---

## License

MIT License — see [LICENSE](LICENSE) for details.
