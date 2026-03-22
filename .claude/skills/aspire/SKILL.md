# Aspire — Polyglot Distributed-App Orchestration

Aspire is a **code-first, polyglot toolchain** for building observable, production-ready distributed applications. It orchestrates containers, executables, and cloud resources from a single AppHost project — regardless of whether the workloads are C#, Python, JavaScript/TypeScript, Go, Java, Rust, Bun, Deno, or PowerShell.

> **Mental model:** The AppHost is a *conductor* — it doesn't play the instruments, it tells every service when to start, how to find each other, and watches for problems.

Detailed reference material lives in the `references/` folder — load on demand.

---

## References

| Reference | When to load |
|---|---|
| [CLI Reference](references/cli-reference.md) | Command flags, options, or detailed usage |
| [MCP Server](references/mcp-server.md) | Setting up MCP for AI assistants, available tools |
| [Integrations Catalog](references/integrations-catalog.md) | Discovering integrations via MCP tools, wiring patterns |
| [Polyglot APIs](references/polyglot-apis.md) | Method signatures, chaining options, language-specific patterns |
| [Architecture](references/architecture.md) | DCP internals, resource model, service discovery, networking, telemetry |
| [Dashboard](references/dashboard.md) | Dashboard features, standalone mode, GenAI Visualizer |
| [Deployment](references/deployment.md) | Docker, Kubernetes, Azure Container Apps, App Service |
| [Testing](references/testing.md) | Integration tests against the AppHost |
| [Troubleshooting](references/troubleshooting.md) | Diagnostic codes, common errors, and fixes |

---

## 1. Researching Aspire Documentation

The Aspire team ships an **MCP server** that provides documentation tools directly inside your AI assistant. See [MCP Server](references/mcp-server.md) for setup details.

### Aspire CLI 13.2+ (recommended — has built-in docs search)

If running Aspire CLI **13.2 or later** (`aspire --version`), the MCP server includes docs search tools:

| Tool | Description |
|---|---|
| `list_docs` | Lists all available documentation from aspire.dev |
| `search_docs` | Performs weighted lexical search across indexed documentation |
| `get_doc` | Retrieves a specific document by its slug |

These tools were added in [PR #14028](https://github.com/dotnet/aspire/pull/14028). To update: `aspire update --self --channel daily`.

For more on this approach, see David Pine's post: https://davidpine.dev/posts/aspire-docs-mcp-tools/

### Aspire CLI 13.1 (integration tools only)

On 13.1, the MCP server provides integration lookup but **not** docs search:

| Tool | Description |
|---|---|
| `list_integrations` | Lists available Aspire hosting integrations |
| `get_integration_docs` | Gets documentation for a specific integration package |

For general docs queries on 13.1, use **Context7** as your primary source (see below).

### Fallback: Context7

Use **Context7** (`mcp_context7`) when the Aspire MCP docs tools are unavailable (13.1) or the MCP server isn't running:

**Step 1 — Resolve the library ID** (one-time per session):

Call `mcp_context7_resolve-library-id` with `libraryName: ".NET Aspire"`.

| Rank | Library ID | Use when |
|---|---|---|
| 1 | `/microsoft/aspire.dev` | Primary source. Guides, integrations, CLI reference, deployment. |
| 2 | `/dotnet/aspire` | API internals, source-level implementation details. |
| 3 | `/communitytoolkit/aspire` | Non-Microsoft polyglot integrations (Go, Java, Node.js, Ollama). |

**Step 2 — Query docs:**

```
libraryId: "/microsoft/aspire.dev", query: "Python integration AddPythonApp service discovery"
libraryId: "/communitytoolkit/aspire", query: "Golang Java Node.js community integrations"
```

### Fallback: GitHub search (when Context7 is also unavailable)

Search the official docs repo on GitHub:
- **Docs repo:** `microsoft/aspire.dev` — path: `src/frontend/src/content/docs/`
- **Source repo:** `dotnet/aspire`
- **Samples repo:** `dotnet/aspire-samples`
- **Community integrations:** `CommunityToolkit/Aspire`

---

## 2. Prerequisites & Install

| Requirement | Details |
|---|---|
| **.NET SDK** | 10.0+ (required even for non-.NET workloads — the AppHost is .NET) |
| **Container runtime** | Docker Desktop, Podman, or Rancher Desktop |
| **IDE (optional)** | VS Code + C# Dev Kit, Visual Studio 2022, JetBrains Rider |

```bash
# Linux / macOS
curl -sSL https://aspire.dev/install.sh | bash

# Windows PowerShell
irm https://aspire.dev/install.ps1 | iex

# Verify
aspire --version

# Install templates
dotnet new install Aspire.ProjectTemplates
```

---

## 3. Project Templates

| Template | Command | Description |
|---|---|---|
| **aspire-starter** | `aspire new aspire-starter` | ASP.NET Core/Blazor starter + AppHost + tests |
| **aspire-ts-cs-starter** | `aspire new aspire-ts-cs-starter` | ASP.NET Core/React starter + AppHost |
| **aspire-py-starter** | `aspire new aspire-py-starter` | FastAPI/React starter + AppHost |
| **aspire-apphost-singlefile** | `aspire new aspire-apphost-singlefile` | Empty single-file AppHost |

---

## 4. AppHost Quick Start (Polyglot)

The AppHost orchestrates all services. Non-.NET workloads run as containers or executables.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("pg").AddDatabase("catalog");

// .NET API
var api = builder.AddProject<Projects.CatalogApi>("api")
    .WithReference(postgres).WithReference(redis);

// Python ML service
var ml = builder.AddPythonApp("ml-service", "../ml-service", "main.py")
    .WithHttpEndpoint(targetPort: 8000).WithReference(redis);

// React frontend (Vite)
var web = builder.AddViteApp("web", "../frontend")
    .WithHttpEndpoint(targetPort: 5173).WithReference(api);

// Go worker
var worker = builder.AddGolangApp("worker", "../go-worker")
    .WithReference(redis);

builder.Build().Run();
```

For complete API signatures, see [Polyglot APIs](references/polyglot-apis.md).

---

## 5. Core Concepts (Summary)

| Concept | Key point |
|---|---|
| **Run vs Publish** | `aspire run` = local dev (DCP engine). `aspire publish` = generate deployment manifests. |
| **Service discovery** | Automatic via env vars: `ConnectionStrings__<name>`, `services__<name>__http__0` |
| **Resource lifecycle** | DAG ordering — dependencies start first. `.WaitFor()` gates on health checks. |
| **Resource types** | `ProjectResource`, `ContainerResource`, `ExecutableResource`, `ParameterResource` |
| **Integrations** | 144+ across 13 categories. Hosting package (AppHost) + Client package (service). |
| **Dashboard** | Real-time logs, traces, metrics, GenAI visualizer. Runs automatically with `aspire run`. |
| **MCP Server** | AI assistants can query running apps and search docs via CLI (STDIO). |
| **Testing** | `Aspire.Hosting.Testing` — spin up full AppHost in xUnit/MSTest/NUnit. |
| **Deployment** | Docker, Kubernetes, Azure Container Apps, Azure App Service. |

---

## 6. CLI Quick Reference

Valid commands in Aspire CLI 13.1:

| Command | Description | Status |
|---|---|---|
| `aspire new <template>` | Create from template | Stable |
| `aspire init` | Initialize in existing project | Stable |
| `aspire run` | Start all resources locally | Stable |
| `aspire add <integration>` | Add an integration | Stable |
| `aspire publish` | Generate deployment manifests | Preview |
| `aspire config` | Manage configuration settings | Stable |
| `aspire cache` | Manage disk cache | Stable |
| `aspire deploy` | Deploy to defined targets | Preview |
| `aspire do <step>` | Execute a pipeline step | Preview |
| `aspire update` | Update integrations (or `--self` for CLI) | Preview |
| `aspire mcp init` | Configure MCP for AI assistants | Stable |
| `aspire mcp start` | Start the MCP server | Stable |

Full command reference with flags: [CLI Reference](references/cli-reference.md).

---

## 7. Common Patterns

### Adding a new service

1. Create your service directory (any language)
2. Add to AppHost: `Add*App()` or `AddProject<T>()`
3. Wire dependencies: `.WithReference()`
4. Gate on health: `.WaitFor()` if needed
5. Run: `aspire run`

### Migrating from Docker Compose

1. `aspire new aspire-apphost-singlefile` (empty AppHost)
2. Replace each `docker-compose` service with an Aspire resource
3. `depends_on` → `.WithReference()` + `.WaitFor()`
4. `ports` → `.WithHttpEndpoint()`
5. `environment` → `.WithEnvironment()` or `.WithReference()`

---

## 8. Key URLs

| Resource | URL |
|---|---|
| **Documentation** | https://aspire.dev |
| **Runtime repo** | https://github.com/dotnet/aspire |
| **Docs repo** | https://github.com/microsoft/aspire.dev |
| **Samples** | https://github.com/dotnet/aspire-samples |
| **Community Toolkit** | https://github.com/CommunityToolkit/Aspire |
| **Dashboard image** | `mcr.microsoft.com/dotnet/aspire-dashboard` |
| **Discord** | https://aka.ms/aspire/discord |
| **Reddit** | https://www.reddit.com/r/aspiredotdev/ |

---

## Reference: architecture

# Architecture — Deep Dive

This reference covers Aspire's internal architecture: the DCP engine, resource model, service discovery, networking, telemetry, and the eventing system.

---

## Developer Control Plane (DCP)

The DCP is the **runtime engine** that Aspire uses in `aspire run` mode. Key facts:

- Written in **Go** (not .NET)
- Exposes a **Kubernetes-compatible API server** (local only, not a real K8s cluster)
- Manages resource lifecycle: create, start, health-check, stop, restart
- Runs containers via the local container runtime (Docker, Podman, Rancher)
- Runs executables as native OS processes
- Handles networking via a proxy layer with automatic port assignment
- Provides the foundation for the Aspire Dashboard's real-time data

### DCP vs Kubernetes

| Aspect | DCP (local dev) | Kubernetes (production) |
|---|---|---|
| API | Kubernetes-compatible | Full Kubernetes API |
| Scope | Single machine | Cluster |
| Networking | Local proxy, auto ports | Service mesh, ingress |
| Storage | Local volumes | PVCs, cloud storage |
| Purpose | Developer inner loop | Production deployment |

The Kubernetes-compatible API means Aspire understands the same resource abstractions, but DCP is **not** a Kubernetes distribution — it's a lightweight local runtime.

---

## Resource Model

Everything in Aspire is a **resource**. The resource model is hierarchical:

### Type hierarchy

```
IResource (interface)
└── Resource (abstract base)
    ├── ProjectResource          — .NET project reference
    ├── ContainerResource        — Docker/OCI container
    ├── ExecutableResource       — Native process (polyglot apps)
    ├── ParameterResource        — Config value or secret
    └── Infrastructure resources
        ├── RedisResource
        ├── PostgresServerResource
        ├── MongoDBServerResource
        ├── SqlServerResource
        ├── RabbitMQServerResource
        ├── KafkaServerResource
        └── ... (one per integration)
```

### Resource properties

Every resource has:
- **Name** — unique identifier within the AppHost
- **State** — lifecycle state (Starting, Running, FailedToStart, Stopping, Stopped, etc.)
- **Annotations** — metadata attached to the resource
- **Endpoints** — network endpoints exposed by the resource
- **Environment variables** — injected into the process/container

### Annotations

Annotations are metadata bags attached to resources. Common built-in annotations:

| Annotation | Purpose |
|---|---|
| `EndpointAnnotation` | Defines an HTTP/HTTPS/TCP endpoint |
| `EnvironmentCallbackAnnotation` | Deferred env var resolution |
| `HealthCheckAnnotation` | Health check configuration |
| `ContainerImageAnnotation` | Docker image details |
| `VolumeAnnotation` | Volume mount configuration |
| `CommandLineArgsCallbackAnnotation` | Dynamic CLI arguments |
| `ManifestPublishingCallbackAnnotation` | Custom publish behavior |

### Resource lifecycle states

```
NotStarted → Starting → Running → Stopping → Stopped
                 ↓                     ↓
          FailedToStart           RuntimeUnhealthy
                                       ↓
                                  Restarting → Running
```

### DAG (Directed Acyclic Graph)

Resources form a dependency graph. Aspire starts resources in topological order:

```
PostgreSQL ──→ API ──→ Frontend
Redis ────────↗
RabbitMQ ──→ Worker
```

1. PostgreSQL, Redis, and RabbitMQ start first (no dependencies)
2. API starts after PostgreSQL and Redis are healthy
3. Frontend starts after API is healthy
4. Worker starts after RabbitMQ is healthy

`.WaitFor()` adds a health-check gate to the dependency edge. Without it, the dependency starts but the downstream doesn't wait for health.

---

## Service Discovery

Aspire injects environment variables into each resource so services can find each other. No service registry or DNS is needed — it's pure environment variable injection.

### Connection strings

For databases, caches, and message brokers:

```
ConnectionStrings__<resource-name>=<connection-string>
```

Examples:
```
ConnectionStrings__cache=localhost:6379
ConnectionStrings__catalog=Host=localhost;Port=5432;Database=catalog;Username=postgres;Password=...
ConnectionStrings__messaging=amqp://guest:guest@localhost:5672
```

### Service endpoints

For HTTP/HTTPS services:

```
services__<resource-name>__<scheme>__0=<url>
```

Examples:
```
services__api__http__0=http://localhost:5234
services__api__https__0=https://localhost:7234
services__ml__http__0=http://localhost:8000
```

### How .WithReference() works

```csharp
var redis = builder.AddRedis("cache");
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(redis);
```

This does:
1. Adds `ConnectionStrings__cache=localhost:<auto-port>` to the API's environment
2. Creates a dependency edge in the DAG (API depends on Redis)
3. In the API service, `builder.Configuration.GetConnectionString("cache")` returns the connection string

### Cross-language service discovery

All languages use the same env var pattern:

| Language | How to read |
|---|---|
| C# | `builder.Configuration.GetConnectionString("cache")` |
| Python | `os.environ["ConnectionStrings__cache"]` |
| JavaScript | `process.env.ConnectionStrings__cache` |
| Go | `os.Getenv("ConnectionStrings__cache")` |
| Java | `System.getenv("ConnectionStrings__cache")` |
| Rust | `std::env::var("ConnectionStrings__cache")` |

---

## Networking

### Proxy architecture

In `aspire run` mode, DCP runs a reverse proxy for each exposed endpoint:

```
Browser → Proxy (auto-assigned port) → Actual Service (target port)
```

- **port** (the external port) — auto-assigned by DCP unless overridden
- **targetPort** — the port your service actually listens on
- All inter-service traffic goes through the proxy for observability

```csharp
// Let DCP auto-assign the external port, service listens on 8000
builder.AddPythonApp("ml", "../ml", "main.py")
    .WithHttpEndpoint(targetPort: 8000);

// Fix the external port to 3000
builder.AddViteApp("web", "../frontend")
    .WithHttpEndpoint(port: 3000, targetPort: 5173);
```

### Endpoint types

```csharp
// HTTP endpoint
.WithHttpEndpoint(port?, targetPort?, name?)

// HTTPS endpoint
.WithHttpsEndpoint(port?, targetPort?, name?)

// Generic endpoint (TCP, custom schemes)
.WithEndpoint(port?, targetPort?, scheme?, name?, isExternal?)

// Mark endpoints as externally accessible (for deployment)
.WithExternalHttpEndpoints()
```

---

## Telemetry (OpenTelemetry)

Aspire configures OpenTelemetry automatically for .NET services. For non-.NET services, you configure OpenTelemetry manually, pointing at the DCP collector.

### What's auto-configured (.NET services)

- **Distributed tracing** — HTTP client/server spans, database spans, messaging spans
- **Metrics** — Runtime metrics, HTTP metrics, custom metrics
- **Structured logging** — Logs correlated with trace context
- **Exporter** — OTLP exporter pointing at the Aspire Dashboard

### Configuring non-.NET services

The DCP exposes an OTLP endpoint. Set these env vars in your non-.NET service:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=<your-service-name>
```

Aspire auto-injects `OTEL_EXPORTER_OTLP_ENDPOINT` via `.WithReference()` for the dashboard collector.

### ServiceDefaults pattern

The `ServiceDefaults` project is a shared configuration library that standardizes:
- OpenTelemetry setup (tracing, metrics, logging)
- Health check endpoints (`/health`, `/alive`)
- Resilience policies (retries, circuit breakers via Polly)

```csharp
// In each .NET service's Program.cs
builder.AddServiceDefaults();   // adds OTel, health checks, resilience
// ... other service config ...
app.MapDefaultEndpoints();      // maps /health and /alive
```

---

## Health Checks

### Built-in health checks

Every integration adds health checks automatically on the client side:
- Redis: `PING` command
- PostgreSQL: `SELECT 1`
- MongoDB: `ping` command
- RabbitMQ: Connection check
- etc.

### WaitFor vs WithReference

```csharp
// WithReference: wires connection string + creates dependency edge
// (downstream may start before dependency is healthy)
.WithReference(db)

// WaitFor: gates on health check — downstream won't start until healthy
.WaitFor(db)

// Typical pattern: both
.WithReference(db).WaitFor(db)
```

### Custom health checks

```csharp
var api = builder.AddProject<Projects.Api>("api")
    .WithHealthCheck("ready", "/health/ready")
    .WithHealthCheck("live", "/health/live");
```

---

## Eventing System

The AppHost supports lifecycle events for reacting to resource state changes:

```csharp
builder.Eventing.Subscribe<ResourceReadyEvent>("api", (evt, ct) =>
{
    // Fires when "api" resource becomes healthy
    Console.WriteLine($"API is ready at {evt.Resource.Name}");
    return Task.CompletedTask;
});

builder.Eventing.Subscribe<BeforeResourceStartedEvent>("db", async (evt, ct) =>
{
    // Run database migrations before the DB resource is marked as started
    await RunMigrations();
});
```

### Available events

| Event | When |
|---|---|
| `BeforeResourceStartedEvent` | Before a resource starts |
| `ResourceReadyEvent` | Resource is healthy and ready |
| `ResourceStateChangedEvent` | Any state transition |
| `BeforeStartEvent` | Before the entire application starts |
| `AfterEndpointsAllocatedEvent` | After all ports are assigned |

---

## Configuration

### Parameters

```csharp
// Plain parameter
var apiKey = builder.AddParameter("api-key");

// Secret parameter (prompted at run, not logged)
var dbPassword = builder.AddParameter("db-password", secret: true);

// Use in resources
var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey);

var db = builder.AddPostgres("db", password: dbPassword);
```

### Configuration sources

Parameters are resolved from (in priority order):
1. Command-line arguments
2. Environment variables
3. User secrets (`dotnet user-secrets`)
4. `appsettings.json` / `appsettings.{Environment}.json`
5. Interactive prompt (for secrets during `aspire run`)

---

## Reference: cli-reference

# CLI Reference — Complete Command Reference

The Aspire CLI (`aspire`) is the primary interface for creating, running, and publishing distributed applications. It is cross-platform and installed standalone (not coupled to the .NET CLI, though `dotnet` commands also work).

**Tested against:** Aspire CLI 13.1.0

---

## Installation

```bash
# Linux / macOS
curl -sSL https://aspire.dev/install.sh | bash

# Windows PowerShell
irm https://aspire.dev/install.ps1 | iex

# Verify
aspire --version

# Update the CLI itself
aspire update --self
```

---

## Global Options

All commands support these options:

| Option                | Description                                    |
| --------------------- | ---------------------------------------------- |
| `-d, --debug`         | Enable debug logging to the console            |
| `--non-interactive`   | Disable all interactive prompts and spinners   |
| `--wait-for-debugger` | Wait for a debugger to attach before executing |
| `-?, -h, --help`      | Show help and usage information                |
| `--version`           | Show version information                       |

---

## Command Reference

### `aspire new`

Create a new project from a template.

```bash
aspire new [<template>] [options]

# Options:
#   -n, --name <name>        Project name
#   -o, --output <dir>       Output directory
#   -s, --source <source>    NuGet source for templates
#   -v, --version <version>  Version of templates to use
#   --channel <channel>      Channel (stable, daily)

# Examples:
aspire new aspire-starter
aspire new aspire-starter -n MyApp -o ./my-app
aspire new aspire-ts-cs-starter
aspire new aspire-py-starter
aspire new aspire-apphost-singlefile
```

Available templates:

- `aspire-starter` — ASP.NET Core/Blazor starter + AppHost + tests
- `aspire-ts-cs-starter` — ASP.NET Core/React + AppHost
- `aspire-py-starter` — FastAPI/React + AppHost
- `aspire-apphost-singlefile` — Empty single-file AppHost

### `aspire init`

Initialize Aspire in an existing project or solution.

```bash
aspire init [options]

# Options:
#   -s, --source <source>    NuGet source for templates
#   -v, --version <version>  Version of templates to use
#   --channel <channel>      Channel (stable, daily)

# Example:
cd my-existing-solution
aspire init
```

Adds AppHost and ServiceDefaults projects to an existing solution. Interactive prompts guide you through selecting which projects to orchestrate.

### `aspire run`

Start all resources locally using the DCP (Developer Control Plane).

```bash
aspire run [options] [-- <additional arguments>]

# Options:
#   --project <path>       Path to AppHost project file

# Examples:
aspire run
aspire run --project ./src/MyApp.AppHost
```

Behavior:

1. Builds the AppHost project
2. Starts the DCP engine
3. Creates resources in dependency order (DAG)
4. Waits for health checks on gated resources
5. Opens the dashboard in the default browser
6. Streams logs to the terminal

Press `Ctrl+C` to gracefully stop all resources.

### `aspire add`

Add a hosting integration to the AppHost.

```bash
aspire add [<integration>] [options]

# Options:
#   --project <path>         Target project file
#   -v, --version <version>  Version of integration to add
#   -s, --source <source>    NuGet source for integration

# Examples:
aspire add redis
aspire add postgresql
aspire add mongodb
```

### `aspire publish` (Preview)

Generate deployment manifests from the AppHost resource model.

```bash
aspire publish [options] [-- <additional arguments>]

# Options:
#   --project <path>                   Path to AppHost project file
#   -o, --output-path <path>           Output directory (default: ./aspire-output)
#   --log-level <level>                Log level (trace, debug, information, warning, error, critical)
#   -e, --environment <env>            Environment (default: Production)
#   --include-exception-details        Include stack traces in pipeline logs

# Examples:
aspire publish
aspire publish --output-path ./deploy
aspire publish -e Staging
```

### `aspire config`

Manage Aspire configuration settings.

```bash
aspire config <subcommand>

# Subcommands:
#   get <key>              Get a configuration value
#   set <key> <value>      Set a configuration value
#   list                   List all configuration values
#   delete <key>           Delete a configuration value

# Examples:
aspire config list
aspire config set telemetry.enabled false
aspire config get telemetry.enabled
aspire config delete telemetry.enabled
```

### `aspire cache`

Manage disk cache for CLI operations.

```bash
aspire cache <subcommand>

# Subcommands:
#   clear                  Clear all cache entries

# Example:
aspire cache clear
```

### `aspire deploy` (Preview)

Deploy the contents of an Aspire apphost to its defined deployment targets.

```bash
aspire deploy [options] [-- <additional arguments>]

# Options:
#   --project <path>                   Path to AppHost project file
#   -o, --output-path <path>           Output path for deployment artifacts
#   --log-level <level>                Log level (trace, debug, information, warning, error, critical)
#   -e, --environment <env>            Environment (default: Production)
#   --include-exception-details        Include stack traces in pipeline logs
#   --clear-cache                      Clear deployment cache for current environment

# Example:
aspire deploy --project ./src/MyApp.AppHost
```

### `aspire do` (Preview)

Execute a specific pipeline step and its dependencies.

```bash
aspire do <step> [options] [-- <additional arguments>]

# Options:
#   --project <path>                   Path to AppHost project file
#   -o, --output-path <path>           Output path for artifacts
#   --log-level <level>                Log level (trace, debug, information, warning, error, critical)
#   -e, --environment <env>            Environment (default: Production)
#   --include-exception-details        Include stack traces in pipeline logs

# Example:
aspire do build-images --project ./src/MyApp.AppHost
```

### `aspire update` (Preview)

Update integrations in the Aspire project, or update the CLI itself.

```bash
aspire update [options]

# Options:
#   --project <path>       Path to AppHost project file
#   --self                 Update the Aspire CLI itself to the latest version
#   --channel <channel>    Channel to update to (stable, daily)

# Examples:
aspire update                          # Update project integrations
aspire update --self                   # Update the CLI itself
aspire update --self --channel daily   # Update CLI to daily build
```

### `aspire mcp`

Manage the MCP (Model Context Protocol) server.

```bash
aspire mcp <subcommand>

# Subcommands:
#   init    Initialize MCP server configuration for detected agent environments
#   start   Start the MCP server
```

#### `aspire mcp init`

```bash
aspire mcp init

# Interactive — detects your AI environment and creates config files.
# Supported environments:
# - VS Code (GitHub Copilot)
# - Copilot CLI
# - Claude Code
# - OpenCode
```

Generates the appropriate configuration file for your detected AI tool.
See [MCP Server](mcp-server.md) for details.

#### `aspire mcp start`

```bash
aspire mcp start

# Starts the MCP server using STDIO transport.
# This is typically invoked by your AI tool, not run manually.
```

---

## Commands That Do NOT Exist

The following commands are **not valid** in Aspire CLI 13.1. Use alternatives:

| Invalid Command | Alternative                                                          |
| --------------- | -------------------------------------------------------------------- |
| `aspire build`  | Use `dotnet build ./AppHost`                                         |
| `aspire test`   | Use `dotnet test ./Tests`                                            |
| `aspire dev`    | Use `aspire run` (includes file watching)                            |
| `aspire list`   | Use `aspire new --help` for templates, `aspire add` for integrations |

---

## .NET CLI equivalents

The `dotnet` CLI can perform some Aspire tasks:

| Aspire CLI                  | .NET CLI Equivalent              |
| --------------------------- | -------------------------------- |
| `aspire new aspire-starter` | `dotnet new aspire-starter`      |
| `aspire run`                | `dotnet run --project ./AppHost` |
| N/A                         | `dotnet build ./AppHost`         |
| N/A                         | `dotnet test ./Tests`            |

The Aspire CLI adds value with `publish`, `deploy`, `add`, `mcp`, `config`, `cache`, `do`, and `update` — commands that have no direct `dotnet` equivalent.

---

## Reference: dashboard

# Dashboard — Complete Reference

The Aspire Dashboard provides real-time observability for all resources in your distributed application. It launches automatically with `aspire run` and can also run standalone.

---

## Features

### Resources view

Displays all resources (projects, containers, executables) with:

- **Name** and **type** (Project, Container, Executable)
- **State** (Starting, Running, Stopped, FailedToStart, etc.)
- **Start time** and **uptime**
- **Endpoints** — clickable URLs for each exposed endpoint
- **Source** — project path, container image, or executable path
- **Actions** — Stop, Start, Restart buttons

### Console logs

Aggregated raw stdout/stderr from all resources:

- Filter by resource name
- Search within logs
- Auto-scroll with pause
- Color-coded by resource

### Structured logs

Application-level structured logs (via ILogger, OpenTelemetry):

- **Filterable** by resource, log level, category, message content
- **Expandable** — click to see full log entry with all properties
- **Correlated** with traces — click to jump to the related trace
- Supports .NET ILogger structured logging properties
- Supports OpenTelemetry log signals from any language

### Distributed traces

End-to-end request traces across all services:

- **Waterfall view** — shows the full call chain with timing
- **Span details** — HTTP method, URL, status code, duration
- **Database spans** — SQL queries, connection details
- **Messaging spans** — queue operations, topic publishes
- **Error highlighting** — failed spans shown in red
- **Cross-service correlation** — trace context propagated automatically for .NET; manual for other languages

### Metrics

Real-time and historical metrics:

- **Runtime metrics** — CPU, memory, GC, thread pool
- **HTTP metrics** — request rate, error rate, latency percentiles
- **Custom metrics** — any metrics your services emit via OpenTelemetry
- **Chartable** — time-series graphs for each metric

### GenAI Visualizer

For applications using AI/LLM integrations:

- **Token usage** — prompt tokens, completion tokens, total tokens per request
- **Prompt/completion pairs** — see the exact prompt sent and response received
- **Model metadata** — which model, temperature, max tokens
- **Latency** — time per AI call
- Requires services to emit [GenAI semantic conventions](https://opentelemetry.io/docs/specs/semconv/gen-ai/) via OpenTelemetry

---

## Dashboard URL

By default, the dashboard runs on an auto-assigned port. Find it:

- In the terminal output when `aspire run` starts
- Via MCP: `list_resources` tool
- Override with `--dashboard-port`:

```bash
aspire run --dashboard-port 18888
```

---

## Standalone Dashboard

Run the dashboard without an AppHost — useful for existing applications that already emit OpenTelemetry:

```bash
docker run --rm -d \
  -p 18888:18888 \
  -p 4317:18889 \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

| Port             | Purpose                                                      |
| ---------------- | ------------------------------------------------------------ |
| `18888`          | Dashboard web UI                                             |
| `4317` → `18889` | OTLP gRPC receiver (standard OTel port → dashboard internal) |

### Configure your services

Point your OpenTelemetry exporters at the dashboard:

```bash
# Environment variables for any language's OpenTelemetry SDK
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_SERVICE_NAME=my-service
```

### Docker Compose example

```yaml
services:
  dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports:
      - "18888:18888"
      - "4317:18889"

  api:
    build: ./api
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://dashboard:18889
      - OTEL_SERVICE_NAME=api

  worker:
    build: ./worker
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://dashboard:18889
      - OTEL_SERVICE_NAME=worker
```

---

## Dashboard configuration

### Authentication

The standalone dashboard supports authentication via browser tokens:

```bash
docker run --rm -d \
  -p 18888:18888 \
  -p 4317:18889 \
  -e DASHBOARD__FRONTEND__AUTHMODE=BrowserToken \
  -e DASHBOARD__FRONTEND__BROWSERTOKEN__TOKEN=my-secret-token \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

### OTLP configuration

```bash
# Accept OTLP over gRPC (default)
-e DASHBOARD__OTLP__GRPC__ENDPOINT=http://0.0.0.0:18889

# Accept OTLP over HTTP
-e DASHBOARD__OTLP__HTTP__ENDPOINT=http://0.0.0.0:18890

# Require API key for OTLP
-e DASHBOARD__OTLP__AUTHMODE=ApiKey
-e DASHBOARD__OTLP__PRIMARYAPIKEY=my-api-key
```

### Resource limits

```bash
# Limit log entries retained
-e DASHBOARD__TELEMETRYLIMITS__MAXLOGCOUNT=10000

# Limit trace entries retained
-e DASHBOARD__TELEMETRYLIMITS__MAXTRACECOUNT=10000

# Limit metric data points
-e DASHBOARD__TELEMETRYLIMITS__MAXMETRICCOUNT=50000
```

---

## Copilot integration

The dashboard integrates with GitHub Copilot in VS Code:

- Ask questions about resource status
- Query logs and traces in natural language
- The MCP server (see [MCP Server](mcp-server.md)) provides the bridge

---

## Non-.NET service telemetry

For non-.NET services to appear in the dashboard, they must emit OpenTelemetry signals. Aspire auto-injects the OTLP endpoint env var when using `.WithReference()`:

### Python (OpenTelemetry SDK)

```python
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
import os

# Aspire injects OTEL_EXPORTER_OTLP_ENDPOINT automatically
endpoint = os.environ.get("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")

provider = TracerProvider()
provider.add_span_processor(BatchSpanProcessor(OTLPSpanExporter(endpoint=endpoint)))
trace.set_tracer_provider(provider)
```

### JavaScript (OpenTelemetry SDK)

```javascript
const { NodeTracerProvider } = require("@opentelemetry/sdk-trace-node");
const { OTLPTraceExporter } = require("@opentelemetry/exporter-trace-otlp-grpc");

const provider = new NodeTracerProvider();
provider.addSpanProcessor(
  new BatchSpanProcessor(
    new OTLPTraceExporter({
      url: process.env.OTEL_EXPORTER_OTLP_ENDPOINT || "http://localhost:4317",
    })
  )
);
provider.register();
```

---

## Reference: deployment

# Deployment — Complete Reference

Aspire separates **orchestration** (what to run) from **deployment** (where to run it). The `aspire publish` command translates your AppHost resource model into deployment manifests for your target platform.

---

## Publish vs Deploy

| Concept | What it does |
|---|---|
| **`aspire publish`** | Generates deployment artifacts (Dockerfiles, Helm charts, Bicep, etc.) |
| **Deploy** | You run the generated artifacts through your CI/CD pipeline |

Aspire does NOT deploy directly. It generates the manifests — you deploy them.

---

## Supported Targets

### Docker

**Package:** `Aspire.Hosting.Docker`

```bash
aspire publish -p docker -o ./docker-output
```

Generates:
- `docker-compose.yml` — service definitions matching your AppHost
- `Dockerfile` for each .NET project
- Environment variable configuration
- Volume mounts
- Network configuration

```csharp
// AppHost configuration for Docker publishing
var api = builder.AddProject<Projects.Api>("api")
    .PublishAsDockerFile();  // override default publish behavior
```

### Kubernetes

**Package:** `Aspire.Hosting.Kubernetes`

```bash
aspire publish -p kubernetes -o ./k8s-output
```

Generates:
- Kubernetes YAML manifests (Deployments, Services, ConfigMaps, Secrets)
- Helm chart (optional)
- Ingress configuration
- Resource limits based on AppHost configuration

```csharp
// AppHost: customize K8s publishing
var api = builder.AddProject<Projects.Api>("api")
    .WithReplicas(3)                    // maps to K8s replicas
    .WithExternalHttpEndpoints();       // maps to Ingress/LoadBalancer
```

### Azure Container Apps

**Package:** `Aspire.Hosting.Azure.AppContainers`

```bash
aspire publish -p azure -o ./azure-output
```

Generates:
- Bicep templates for Azure Container Apps Environment
- Container App definitions for each service
- Azure Container Registry configuration
- Managed identity configuration
- Dapr components (if using Dapr integration)
- VNET configuration

```csharp
// AppHost: Azure-specific configuration
var api = builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints()        // maps to external ingress
    .WithReplicas(3);                   // maps to min replicas

// Azure resources are auto-provisioned
var storage = builder.AddAzureStorage("storage");   // creates Storage Account
var cosmos = builder.AddAzureCosmosDB("cosmos");    // creates Cosmos DB account
var sb = builder.AddAzureServiceBus("messaging");   // creates Service Bus namespace
```

### Azure App Service

**Package:** `Aspire.Hosting.Azure.AppService`

```bash
aspire publish -p appservice -o ./appservice-output
```

Generates:
- Bicep templates for App Service Plans and Web Apps
- Connection string configuration
- Application settings

---

## Resource model to deployment mapping

| AppHost concept | Docker Compose | Kubernetes | Azure Container Apps |
|---|---|---|---|
| `AddProject<T>()` | `service` with Dockerfile | `Deployment` + `Service` | `Container App` |
| `AddContainer()` | `service` with `image:` | `Deployment` + `Service` | `Container App` |
| `AddRedis()` | `service: redis` | `StatefulSet` | Managed Redis |
| `AddPostgres()` | `service: postgres` | `StatefulSet` | Azure PostgreSQL |
| `.WithReference()` | `environment:` vars | `ConfigMap` / `Secret` | App settings |
| `.WithReplicas(n)` | `deploy: replicas: n` | `replicas: n` | `minReplicas: n` |
| `.WithVolume()` | `volumes:` | `PersistentVolumeClaim` | Azure Files |
| `.WithHttpEndpoint()` | `ports:` | `Service` port | Ingress |
| `.WithExternalHttpEndpoints()` | `ports:` (host) | `Ingress` / `LoadBalancer` | External ingress |
| `AddParameter(secret: true)` | `.env` file | `Secret` | Key Vault reference |

---

## CI/CD integration

### GitHub Actions example

```yaml
name: Deploy
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install Aspire CLI
        run: curl -sSL https://aspire.dev/install.sh | bash

      - name: Generate manifests
        run: aspire publish -p azure -o ./deploy

      - name: Deploy to Azure
        uses: azure/arm-deploy@v2
        with:
          template: ./deploy/main.bicep
          parameters: ./deploy/main.parameters.json
```

### Azure DevOps example

```yaml
trigger:
  branches:
    include: [main]

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: curl -sSL https://aspire.dev/install.sh | bash
    displayName: 'Install Aspire CLI'

  - script: aspire publish -p azure -o $(Build.ArtifactStagingDirectory)/deploy
    displayName: 'Generate deployment manifests'

  - task: AzureResourceManagerTemplateDeployment@3
    inputs:
      deploymentScope: 'Resource Group'
      templateLocation: '$(Build.ArtifactStagingDirectory)/deploy/main.bicep'
```

---

## Environment-specific configuration

### Using parameters for secrets

```csharp
// AppHost
var dbPassword = builder.AddParameter("db-password", secret: true);
var postgres = builder.AddPostgres("db", password: dbPassword);
```

In deployment:
- **Docker:** Loaded from `.env` file
- **Kubernetes:** Loaded from `Secret` resource
- **Azure:** Loaded from Key Vault via managed identity

### Conditional resources

```csharp
// Use Azure services in production, emulators locally
if (builder.ExecutionContext.IsPublishMode)
{
    var cosmos = builder.AddAzureCosmosDB("cosmos");    // real Azure resource
}
else
{
    var cosmos = builder.AddAzureCosmosDB("cosmos")
        .RunAsEmulator();                                // local emulator
}
```

---

## Dev Containers & GitHub Codespaces

Aspire templates include `.devcontainer/` configuration:

```json
{
  "name": "Aspire App",
  "image": "mcr.microsoft.com/devcontainers/dotnet:10.0",
  "features": {
    "ghcr.io/devcontainers/features/docker-in-docker:2": {},
    "ghcr.io/devcontainers/features/node:1": {}
  },
  "postCreateCommand": "curl -sSL https://aspire.dev/install.sh | bash",
  "forwardPorts": [18888],
  "portsAttributes": {
    "18888": { "label": "Aspire Dashboard" }
  }
}
```

Port forwarding works automatically in Codespaces — the dashboard and all service endpoints are accessible via forwarded URLs.

---

## Reference: integrations-catalog

# Integrations Catalog

Aspire has **144+ integrations** across 13 categories. Rather than maintaining a static list, use the MCP tools to get live, up-to-date integration data.

---

## Discovering integrations (MCP tools)

The Aspire MCP server provides two tools for integration discovery — these work on **all CLI versions** (13.1+) and do **not** require a running AppHost.

| Tool                   | What it does                                                                                             | When to use                                                                                 |
| ---------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| `list_integrations`    | Returns all available Aspire hosting integrations with their NuGet package IDs                           | "What integrations are available for databases?" / "Show me all Redis-related integrations" |
| `get_integration_docs` | Retrieves detailed documentation for a specific integration package (setup, configuration, code samples) | "How do I configure PostgreSQL?" / "Show me the docs for `Aspire.Hosting.Redis`"            |

### Workflow

1. **Browse** — Call `list_integrations` to see what's available. Filter results by category or keyword.
2. **Deep dive** — Call `get_integration_docs` with the package ID (e.g., `Aspire.Hosting.Redis`) and version (e.g., `9.0.0`) to get full setup instructions.
3. **Add** — Run `aspire add <integration>` to install the hosting package into your AppHost.

> **Tip:** These tools return the same data as the [official integrations gallery](https://aspire.dev/integrations/gallery/). Prefer them over static docs — integrations are added frequently.

---

## Integration pattern

Every integration follows a two-package pattern:

- **Hosting package** (`Aspire.Hosting.*`) — adds the resource to the AppHost
- **Client package** (`Aspire.*`) — configures the client SDK in your service with health checks, telemetry, and retries
- **Community Toolkit** (`CommunityToolkit.Aspire.*`) — community-maintained integrations from [Aspire Community Toolkit](https://github.com/CommunityToolkit/Aspire)

```csharp
// === AppHost (hosting side) ===
var redis = builder.AddRedis("cache");  // Aspire.Hosting.Redis
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(redis);

// === Service (client side) — in API's Program.cs ===
builder.AddRedisClient("cache");        // Aspire.StackExchange.Redis
// Automatically configures: connection string, health checks, OpenTelemetry, retries
```

---

## Categories at a glance

Use `list_integrations` for the full live list. This summary covers the major categories:

| Category            | Key integrations                                                                      | Example hosting package                  |
| ------------------- | ------------------------------------------------------------------------------------- | ---------------------------------------- |
| **AI**              | Azure OpenAI, OpenAI, GitHub Models, Ollama                                           | `Aspire.Hosting.Azure.CognitiveServices` |
| **Caching**         | Redis, Garnet, Valkey, Azure Cache for Redis                                          | `Aspire.Hosting.Redis`                   |
| **Cloud / Azure**   | Storage, Cosmos DB, Service Bus, Key Vault, Event Hubs, Functions, SQL, SignalR (25+) | `Aspire.Hosting.Azure.Storage`           |
| **Cloud / AWS**     | AWS SDK integration                                                                   | `Aspire.Hosting.AWS`                     |
| **Databases**       | PostgreSQL, SQL Server, MongoDB, MySQL, Oracle, Elasticsearch, Milvus, Qdrant, SQLite | `Aspire.Hosting.PostgreSQL`              |
| **DevTools**        | Data API Builder, Dev Tunnels, Mailpit, k6, Flagd, Ngrok, Stripe                      | `Aspire.Hosting.DevTunnels`              |
| **Messaging**       | RabbitMQ, Kafka, NATS, ActiveMQ, LavinMQ                                              | `Aspire.Hosting.RabbitMQ`                |
| **Observability**   | OpenTelemetry (built-in), Seq, OTel Collector                                         | `Aspire.Hosting.Seq`                     |
| **Compute**         | Docker Compose, Kubernetes                                                            | `Aspire.Hosting.Docker`                  |
| **Reverse Proxies** | YARP                                                                                  | `Aspire.Hosting.Yarp`                    |
| **Security**        | Keycloak                                                                              | `Aspire.Hosting.Keycloak`                |
| **Frameworks**      | JavaScript, Python, Go, Java, Rust, Bun, Deno, Orleans, MAUI, Dapr, PowerShell        | `Aspire.Hosting.Python`                  |

For polyglot framework method signatures, see [Polyglot APIs](polyglot-apis.md).

---

---

## Reference: mcp-server

# MCP Server — Complete Reference

Aspire exposes an **MCP (Model Context Protocol) server** that lets AI coding assistants query and control your running distributed application, and search Aspire documentation. This enables AI tools to inspect resource status, read logs, view traces, restart services, and look up docs — all from within the AI assistant's context.

Reference: https://aspire.dev/get-started/configure-mcp/

---

## Setup: `aspire mcp init`

The easiest way to configure the MCP server is using the Aspire CLI:

```bash
# Open a terminal in your project directory
aspire mcp init
```

The command walks you through an interactive setup:

1. **Workspace root** — prompts for the path to your workspace root (defaults to current directory)
2. **Environment detection** — detects supported AI environments (VS Code, Copilot CLI, Claude Code, OpenCode) and asks which to configure
3. **Playwright MCP** — optionally offers to configure the Playwright MCP server alongside Aspire
4. **Config creation** — writes the appropriate configuration files (e.g., `.vscode/mcp.json`)
5. **AGENTS.md** — if one doesn't already exist, creates an `AGENTS.md` with Aspire-specific instructions for AI agents

> **Note:** `aspire mcp init` uses interactive prompts (Spectre.Console). It must be run in a real terminal — the VS Code integrated terminal may not handle the prompts correctly. Use an external terminal if needed.

---

## Understanding the Configuration

When you run `aspire mcp init`, the CLI creates configuration files appropriate for your detected environment.

### VS Code (GitHub Copilot)

Creates or updates `.vscode/mcp.json`:

```json
{
  "servers": {
    "aspire": {
      "type": "stdio",
      "command": "aspire",
      "args": ["mcp", "start"]
    }
  }
}
```

## MCP Tools

The tools available depend on your Aspire CLI version. Check with `aspire --version`.

### Tools available in 13.1+ (stable)

#### Resource management tools

These tools require a running AppHost (`aspire run`).

| Tool                         | Description                                                                          |
| ---------------------------- | ------------------------------------------------------------------------------------ |
| `list_resources`             | Lists all resources, including state, health status, source, endpoints, and commands                |
| `list_console_logs`          | Lists console logs for a resource                                                                   |
| `list_structured_logs`       | Lists structured logs, optionally filtered by resource name                                         |
| `list_traces`                | Lists distributed traces. Traces can be filtered using an optional resource name parameter          |
| `list_trace_structured_logs` | Lists structured logs for a specific trace                                                          |
| `execute_resource_command`   | Executes a resource command (accepts resource name and command name)                                |

#### AppHost management tools

| Tool             | Description                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------------- |
| `list_apphosts`  | Lists all detected AppHost connections, showing which are in/out of working directory scope |
| `select_apphost` | Selects which AppHost to use when multiple are running                                      |

#### Integration tools

These work without a running AppHost.

| Tool                   | Description                                                                                                       |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `list_integrations`    | Lists available Aspire hosting integrations (NuGet packages for databases, message brokers, cloud services, etc.) |
| `get_integration_docs` | Gets documentation for a specific Aspire hosting integration package                                              |

### Tools added in 13.2+ (documentation search)

> **Version gate:** These tools were added in [PR #14028](https://github.com/dotnet/aspire/pull/14028) and ship in Aspire CLI **13.2**. If you are on 13.1, these tools will NOT appear. To get them early, update to the daily channel: `aspire update --self --channel daily`.

| Tool          | Description                                                              |
| ------------- | ------------------------------------------------------------------------ |
| `list_docs`   | Lists all available documentation from aspire.dev                        |
| `search_docs` | Performs weighted lexical search across indexed aspire.dev documentation |
| `get_doc`     | Retrieves a specific document by its slug                                |

These tools index aspire.dev content using the `llms.txt` specification and provide weighted lexical search (titles 10x, summaries 8x, headings 6x, code 5x, body 1x). They work without a running AppHost.

### Fallback for documentation (13.1 users)

If you are on Aspire CLI 13.1 and don't have `list_docs`/`search_docs`/`get_doc`, use **Context7** as a fallback for documentation queries. See the [SKILL.md documentation research section](../SKILL.md#1-researching-aspire-documentation) for details.

---

## Excluding Resources from MCP

Resources and associated telemetry can be excluded from MCP results by annotating the resource:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Api>("apiservice")
    .ExcludeFromMcp();  // Hidden from MCP tools

builder.AddProject<Projects.Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
```

---

## Supported AI Assistants

The `aspire mcp init` command supports:

- [VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers) (GitHub Copilot)
- [Copilot CLI](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/use-copilot-cli#add-an-mcp-server)
- [Claude Code](https://docs.claude.com/en/docs/claude-code/mcp)
- [OpenCode](https://opencode.ai/docs/mcp-servers/)

The MCP server uses the **STDIO transport protocol** and may work with other agentic coding environments that support this protocol.

---

## Usage Patterns

### Debugging with AI assistance

Once MCP is configured, your AI assistant can:

1. **Inspect running state:**

   - "List all my Aspire resources and their status"
   - "Is the database healthy?"
   - "What port is the API running on?"

2. **Read logs:**

   - "Show me the recent logs from the ML service"
   - "Are there any errors in the worker logs?"

3. **View traces:**

   - "Show me the trace for the last failed request"
   - "What's the latency for API → Database calls?"

4. **Control resources:**

   - "Restart the API service"
   - "Stop the worker while I debug the queue"

5. **Search docs (13.2+):**
   - "Search the Aspire docs for Redis caching"
   - "How do I configure service discovery?"
   - _(Requires CLI 13.2+. On 13.1, use Context7 or `list_integrations`/`get_integration_docs` for integration-specific docs.)_

---

## Security Considerations

- The MCP server only exposes resources from the local AppHost
- No authentication is required (local development only)
- The STDIO transport only works for the AI tool that spawned the process
- **Do not expose the MCP endpoint to the network in production**

---

## Limitations

- AI models have limits on data processing. Large data fields (e.g., stack traces) may be truncated.
- Requests involving large collections of telemetry may be shortened by omitting older items.

---

## Troubleshooting

If you run into issues, check the [open MCP issues on GitHub](https://github.com/dotnet/aspire/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-mcp).

## See Also

- [aspire mcp command](https://aspire.dev/reference/cli/commands/aspire-mcp/)
- [aspire mcp init command](https://aspire.dev/reference/cli/commands/aspire-mcp-init/)
- [aspire mcp start command](https://aspire.dev/reference/cli/commands/aspire-mcp-start/)
- [GitHub Copilot in the Dashboard](https://aspire.dev/dashboard/copilot/)
- [How I taught AI to read Aspire docs](https://davidpine.dev/posts/aspire-docs-mcp-tools/)

---

## Reference: polyglot-apis

# Polyglot APIs — Complete Reference

Aspire supports 10+ languages/runtimes. The AppHost is always .NET, but orchestrated workloads can be any language. Each language has a hosting method that returns a resource you wire into the dependency graph.

---

## Hosting model differences

| Model | Resource type | How it runs | Examples |
|---|---|---|---|
| **Project** | `ProjectResource` | .NET project reference, built by SDK | `AddProject<T>()` |
| **Container** | `ContainerResource` | Docker/OCI image | `AddContainer()`, `AddRedis()`, `AddPostgres()` |
| **Executable** | `ExecutableResource` | Native OS process | `AddExecutable()`, all `Add*App()` polyglot methods |

All polyglot `Add*App()` methods create `ExecutableResource` instances under the hood. They don't require the target language's SDK on the AppHost side — only that the workload's runtime is installed on the dev machine.

---

## Official (Microsoft-maintained)

### .NET / C\#

```csharp
builder.AddProject<Projects.MyApi>("api")
```

**Chaining methods:**
- `.WithHttpEndpoint(port?, targetPort?, name?)` — expose HTTP endpoint
- `.WithHttpsEndpoint(port?, targetPort?, name?)` — expose HTTPS endpoint
- `.WithEndpoint(port?, targetPort?, scheme?, name?)` — generic endpoint
- `.WithReference(resource)` — wire dependency (connection string or service discovery)
- `.WithReplicas(count)` — run multiple instances
- `.WithEnvironment(key, value)` — set environment variable
- `.WithEnvironment(callback)` — set env vars via callback (deferred resolution)
- `.WaitFor(resource)` — don't start until dependency is healthy
- `.WithExternalHttpEndpoints()` — mark endpoints as externally accessible
- `.WithOtlpExporter()` — configure OpenTelemetry exporter
- `.PublishAsDockerFile()` — override publish behavior to Dockerfile

### Python

```csharp
// Standard Python script
builder.AddPythonApp("service", "../python-service", "main.py")

// Uvicorn ASGI server (FastAPI, Starlette, etc.)
builder.AddUvicornApp("fastapi", "../fastapi-app", "app:app")
```

**`AddPythonApp(name, projectDirectory, scriptPath, args?)`**

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)` — expose HTTP
- `.WithVirtualEnvironment(path?)` — use venv (default: `.venv`)
- `.WithPipPackages(packages)` — install pip packages on start
- `.WithReference(resource)` — wire dependency
- `.WithEnvironment(key, value)` — set env var
- `.WaitFor(resource)` — wait for dependency health

**`AddUvicornApp(name, projectDirectory, appModule, args?)`**

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)` — expose HTTP
- `.WithVirtualEnvironment(path?)` — use venv
- `.WithReference(resource)` — wire dependency
- `.WithEnvironment(key, value)` — set env var
- `.WaitFor(resource)` — wait for dependency health

**Python service discovery:** Environment variables are injected automatically. Use `os.environ` to read:
```python
import os
redis_conn = os.environ["ConnectionStrings__cache"]
api_url = os.environ["services__api__http__0"]
```

### JavaScript / TypeScript

```csharp
// Generic JavaScript app (npm start)
builder.AddJavaScriptApp("frontend", "../web-app")

// Vite dev server
builder.AddViteApp("spa", "../vite-app")

// Node.js script
builder.AddNodeApp("worker", "server.js", "../node-worker")
```

**`AddJavaScriptApp(name, workingDirectory)`**

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)` — expose HTTP
- `.WithNpmPackageInstallation()` — run `npm install` before start
- `.WithReference(resource)` — wire dependency
- `.WithEnvironment(key, value)` — set env var
- `.WaitFor(resource)` — wait for dependency health

**`AddViteApp(name, workingDirectory)`**

Chaining methods (same as `AddJavaScriptApp` plus):
- `.WithNpmPackageInstallation()` — run `npm install` before start
- `.WithHttpEndpoint(port?, targetPort?, name?)` — Vite defaults to 5173

**`AddNodeApp(name, scriptPath, workingDirectory)`**

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)` — expose HTTP
- `.WithNpmPackageInstallation()` — run `npm install` before start
- `.WithReference(resource)` — wire dependency
- `.WithEnvironment(key, value)` — set env var

**JS/TS service discovery:** Environment variables are injected. Use `process.env`:
```javascript
const redisUrl = process.env.ConnectionStrings__cache;
const apiUrl = process.env.services__api__http__0;
```

---

## Community (CommunityToolkit/Aspire)

All community integrations follow the same pattern: install the NuGet package in your AppHost, then use the `Add*App()` method.

### Go

**Package:** `CommunityToolkit.Aspire.Hosting.Golang`

```csharp
builder.AddGolangApp("go-api", "../go-service")
    .WithHttpEndpoint(targetPort: 8080)
    .WithReference(redis)
    .WithEnvironment("LOG_LEVEL", "debug")
    .WaitFor(redis);
```

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)`
- `.WithReference(resource)`
- `.WithEnvironment(key, value)`
- `.WaitFor(resource)`

**Go service discovery:** Standard env vars via `os.Getenv()`:
```go
redisAddr := os.Getenv("ConnectionStrings__cache")
```

### Java (Spring Boot)

**Package:** `CommunityToolkit.Aspire.Hosting.Java`

```csharp
builder.AddSpringApp("spring-api", "../spring-service")
    .WithHttpEndpoint(targetPort: 8080)
    .WithReference(postgres)
    .WaitFor(postgres);
```

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)`
- `.WithReference(resource)`
- `.WithEnvironment(key, value)`
- `.WaitFor(resource)`
- `.WithMavenBuild()` — run Maven build before start
- `.WithGradleBuild()` — run Gradle build before start

**Java service discovery:** Env vars via `System.getenv()`:
```java
String dbConn = System.getenv("ConnectionStrings__db");
```

### Rust

**Package:** `CommunityToolkit.Aspire.Hosting.Rust`

```csharp
builder.AddRustApp("rust-worker", "../rust-service")
    .WithHttpEndpoint(targetPort: 3000)
    .WithReference(redis)
    .WaitFor(redis);
```

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)`
- `.WithReference(resource)`
- `.WithEnvironment(key, value)`
- `.WaitFor(resource)`
- `.WithCargoBuild()` — run `cargo build` before start

### Bun

**Package:** `CommunityToolkit.Aspire.Hosting.Bun`

```csharp
builder.AddBunApp("bun-api", "../bun-service")
    .WithHttpEndpoint(targetPort: 3000)
    .WithReference(redis);
```

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)`
- `.WithReference(resource)`
- `.WithEnvironment(key, value)`
- `.WaitFor(resource)`
- `.WithBunPackageInstallation()` — run `bun install` before start

### Deno

**Package:** `CommunityToolkit.Aspire.Hosting.Deno`

```csharp
builder.AddDenoApp("deno-api", "../deno-service")
    .WithHttpEndpoint(targetPort: 8000)
    .WithReference(redis);
```

Chaining methods:
- `.WithHttpEndpoint(port?, targetPort?, name?)`
- `.WithReference(resource)`
- `.WithEnvironment(key, value)`
- `.WaitFor(resource)`

### PowerShell

```csharp
builder.AddPowerShell("ps-script", "../scripts/process.ps1")
    .WithReference(storageAccount);
```

### Dapr

**Package:** `Aspire.Hosting.Dapr` (official)

```csharp
var dapr = builder.AddDapr();
var api = builder.AddProject<Projects.Api>("api")
    .WithDaprSidecar("api-sidecar");
```

---

## Complete mixed-language example

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("pg").AddDatabase("catalog");
var mongo = builder.AddMongoDB("mongo").AddDatabase("analytics");
var rabbit = builder.AddRabbitMQ("messaging");

// .NET API (primary)
var api = builder.AddProject<Projects.CatalogApi>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbit)
    .WaitFor(postgres)
    .WaitFor(redis);

// Python ML service (FastAPI)
var ml = builder.AddUvicornApp("ml", "../ml-service", "app:app")
    .WithHttpEndpoint(targetPort: 8000)
    .WithVirtualEnvironment()
    .WithReference(redis)
    .WithReference(mongo)
    .WaitFor(redis);

// TypeScript frontend (Vite + React)
var web = builder.AddViteApp("web", "../frontend")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(targetPort: 5173)
    .WithReference(api);

// Go event processor
var processor = builder.AddGolangApp("processor", "../go-processor")
    .WithReference(rabbit)
    .WithReference(mongo)
    .WaitFor(rabbit);

// Java analytics service (Spring Boot)
var analytics = builder.AddSpringApp("analytics", "../spring-analytics")
    .WithHttpEndpoint(targetPort: 8080)
    .WithReference(mongo)
    .WithReference(rabbit)
    .WaitFor(mongo);

// Rust high-perf worker
var worker = builder.AddRustApp("worker", "../rust-worker")
    .WithReference(redis)
    .WithReference(rabbit)
    .WaitFor(redis);

builder.Build().Run();
```

This single AppHost starts 6 services across 5 languages plus 4 infrastructure resources, all wired together with automatic service discovery.

---

## Reference: testing

# Testing — Complete Reference

Aspire provides `Aspire.Hosting.Testing` for running integration tests against your full AppHost. Tests spin up the entire distributed application (or a subset) and run assertions against real services.

---

## Package

```xml
<PackageReference Include="Aspire.Hosting.Testing" Version="*" />
```

---

## Core pattern: DistributedApplicationTestingBuilder

```csharp
// 1. Create a testing builder from your AppHost
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.MyAppHost>();

// 2. (Optional) Override resources for testing
// ... see customization section below

// 3. Build and start the application
await using var app = await builder.BuildAsync();
await app.StartAsync();

// 4. Create HTTP clients for your services
var client = app.CreateHttpClient("api");

// 5. Run assertions
var response = await client.GetAsync("/health");
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
```

---

## xUnit examples

### Basic health check test

```csharp
public class HealthTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AllServicesAreHealthy()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Test each service's health endpoint
        var apiClient = app.CreateHttpClient("api");
        var apiHealth = await apiClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, apiHealth.StatusCode);

        var workerClient = app.CreateHttpClient("worker");
        var workerHealth = await workerClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, workerHealth.StatusCode);
    }
}
```

### API integration test

```csharp
public class ApiTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateOrder_ReturnsCreated()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var client = app.CreateHttpClient("api");

        var order = new { ProductId = 1, Quantity = 2 };
        var response = await client.PostAsJsonAsync("/orders", order);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Order>();
        Assert.NotNull(created);
        Assert.Equal(1, created.ProductId);
    }
}
```

### Testing with wait for readiness

```csharp
[Fact]
public async Task DatabaseIsSeeded()
{
    var builder = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.AppHost>();

    await using var app = await builder.BuildAsync();
    await app.StartAsync();

    // Wait for the API to be fully ready (all dependencies healthy)
    await app.WaitForResourceReadyAsync("api");

    var client = app.CreateHttpClient("api");
    var response = await client.GetAsync("/products");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var products = await response.Content.ReadFromJsonAsync<List<Product>>();
    Assert.NotEmpty(products);
}
```

---

## MSTest examples

```csharp
[TestClass]
public class IntegrationTests
{
    [TestMethod]
    public async Task ApiReturnsProducts()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var client = app.CreateHttpClient("api");
        var response = await client.GetAsync("/products");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
```

---

## NUnit examples

```csharp
[TestFixture]
public class IntegrationTests
{
    [Test]
    public async Task ApiReturnsProducts()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var client = app.CreateHttpClient("api");
        var response = await client.GetAsync("/products");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
```

---

## Customizing the test AppHost

### Override resources

```csharp
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.AppHost>();

// Replace a real database with a test container
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
});

// Add test-specific configuration
builder.Configuration["TestMode"] = "true";

await using var app = await builder.BuildAsync();
await app.StartAsync();
```

### Exclude resources

```csharp
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.AppHost>(args =>
    {
        // Don't start the worker for API-only tests
        args.Args = ["--exclude-resource", "worker"];
    });
```

### Test with specific environment

```csharp
var builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.AppHost>(args =>
    {
        args.Args = ["--environment", "Testing"];
    });
```

---

## Connection string access

```csharp
// Get the connection string for a resource in tests
var connectionString = await app.GetConnectionStringAsync("db");

// Use it to query the database directly in tests
using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();
var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM products");
Assert.True(count > 0);
```

---

## Best practices

1. **Use `WaitForResourceReadyAsync`** before making requests — ensures all dependencies are healthy
2. **Each test should be independent** — don't rely on state from previous tests
3. **Use `await using`** for the app — ensures cleanup even on test failure
4. **Test real infrastructure** — Aspire spins up real containers (Redis, PostgreSQL, etc.), giving you high-fidelity integration tests
5. **Keep test AppHost lean** — exclude resources you don't need for specific test scenarios
6. **Use test-specific configuration** — override settings for test isolation
7. **Timeout protection** — set reasonable test timeouts since containers take time to start:

```csharp
[Fact(Timeout = 120_000)]  // 2 minutes
public async Task SlowIntegrationTest() { ... }
```

---

## Project structure

```
MyApp/
├── src/
│   ├── MyApp.AppHost/           # AppHost project
│   ├── MyApp.Api/               # API service
│   ├── MyApp.Worker/            # Worker service
│   └── MyApp.ServiceDefaults/   # Shared defaults
└── tests/
    └── MyApp.Tests/             # Integration tests
        ├── MyApp.Tests.csproj   # References AppHost + Testing package
        └── ApiTests.cs          # Test classes
```

```xml
<!-- MyApp.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsAspireTestProject>true</IsAspireTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" Version="*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="*" />
    <PackageReference Include="xunit" Version="*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MyApp.AppHost\MyApp.AppHost.csproj" />
  </ItemGroup>
</Project>
```

---

## Reference: troubleshooting

# Troubleshooting — Diagnostics & Common Issues

---

## Diagnostic Codes

Aspire emits diagnostic codes for common issues. These appear in build warnings/errors and IDE diagnostics.

### Standard diagnostics

| Code          | Severity | Description                                                |
| ------------- | -------- | ---------------------------------------------------------- |
| **ASPIRE001** | Warning  | Resource name contains invalid characters                  |
| **ASPIRE002** | Warning  | Duplicate resource name detected                           |
| **ASPIRE003** | Error    | Missing required package reference                         |
| **ASPIRE004** | Warning  | Deprecated API usage                                       |
| **ASPIRE005** | Error    | Invalid endpoint configuration                             |
| **ASPIRE006** | Warning  | Health check not configured for resource with `.WaitFor()` |
| **ASPIRE007** | Warning  | Container image tag not specified (using `latest`)         |
| **ASPIRE008** | Error    | Circular dependency detected in resource graph             |

### Experimental diagnostics (ASPIREHOSTINGX\*)

These codes indicate usage of experimental/preview APIs. They may require `#pragma warning disable` or `<NoWarn>` if you intentionally use experimental features:

| Code                      | Area                             |
| ------------------------- | -------------------------------- |
| ASPIRE_HOSTINGX_0001–0005 | Experimental hosting APIs        |
| ASPIRE_HOSTINGX_0006–0010 | Experimental integration APIs    |
| ASPIRE_HOSTINGX_0011–0015 | Experimental deployment APIs     |
| ASPIRE_HOSTINGX_0016–0022 | Experimental resource model APIs |

To suppress experimental warnings:

```xml
<!-- In .csproj -->
<PropertyGroup>
  <NoWarn>$(NoWarn);ASPIRE_HOSTINGX_0001</NoWarn>
</PropertyGroup>
```

Or per-line:

```csharp
#pragma warning disable ASPIRE_HOSTINGX_0001
var resource = builder.AddExperimentalResource("test");
#pragma warning restore ASPIRE_HOSTINGX_0001
```

---

## Common Issues & Solutions

### Container runtime

| Problem                           | Solution                                                                                               |
| --------------------------------- | ------------------------------------------------------------------------------------------------------ |
| "Cannot connect to Docker daemon" | Start Docker Desktop / Podman / Rancher Desktop                                                        |
| Container fails to start          | Check `docker ps -a` for exit codes; check dashboard console logs                                      |
| Port already in use               | Another process is using the port; Aspire auto-assigns, but `targetPort` must be free on the container |
| Container image pull fails        | Check network connectivity; verify image name and tag                                                  |
| "Permission denied" on Linux      | Add user to `docker` group: `sudo usermod -aG docker $USER`                                            |

### Service discovery

| Problem                       | Solution                                                                     |
| ----------------------------- | ---------------------------------------------------------------------------- |
| Service can't find dependency | Verify `.WithReference()` in AppHost; check env vars in dashboard            |
| Connection string is null     | The reference resource name doesn't match; check `ConnectionStrings__<name>` |
| Wrong port in service URL     | Check `targetPort` vs actual service listen port                             |
| Env var not set               | Rebuild AppHost; verify resource name matches exactly                        |

### Python workloads

| Problem                           | Solution                                                        |
| --------------------------------- | --------------------------------------------------------------- |
| "Python not found"                | Ensure Python is on PATH; specify full path in `AddPythonApp()` |
| venv not found                    | Use `.WithVirtualEnvironment()` or create venv manually         |
| pip packages fail to install      | Use `.WithPipPackages()` or install in venv before `aspire run` |
| ModuleNotFoundError               | venv isn't activated; `.WithVirtualEnvironment()` handles this  |
| "Port already in use" for Uvicorn | Check `targetPort` — another instance may be running            |

### JavaScript / TypeScript workloads

| Problem                       | Solution                                                         |
| ----------------------------- | ---------------------------------------------------------------- |
| "node_modules not found"      | Use `.WithNpmPackageInstallation()` to auto-install              |
| npm install fails             | Check `package.json` is valid; check npm registry connectivity   |
| Vite dev server won't start   | Verify `vite` is in devDependencies; check Vite config           |
| Port mismatch                 | Ensure `targetPort` matches the port in your JS framework config |
| TypeScript compilation errors | These happen in the service, not Aspire — check service logs     |

### Go workloads

| Problem                    | Solution                                                   |
| -------------------------- | ---------------------------------------------------------- |
| "go not found"             | Ensure Go is installed and on PATH                         |
| Build fails                | Check `go.mod` exists in working directory                 |
| "no Go files in directory" | Verify `workingDir` points to the directory with `main.go` |

### Java workloads

| Problem                  | Solution                                                |
| ------------------------ | ------------------------------------------------------- |
| "java not found"         | Ensure JDK is installed and `JAVA_HOME` is set          |
| Maven/Gradle build fails | Verify build files exist; check build tool installation |
| Spring Boot won't start  | Check `application.properties`; verify main class       |

### Rust workloads

| Problem              | Solution                                                             |
| -------------------- | -------------------------------------------------------------------- |
| "cargo not found"    | Install Rust via rustup                                              |
| Build takes too long | Rust compile times are normal; use `.WithCargoBuild()` for pre-build |

### Health checks & startup

| Problem                      | Solution                                                                       |
| ---------------------------- | ------------------------------------------------------------------------------ |
| Resource stuck in "Starting" | Health check endpoint not responding; check service logs                       |
| `.WaitFor()` timeout         | Increase timeout or fix health endpoint; default is 30 seconds                 |
| Health check always fails    | Verify endpoint path (default: `/health`); check service binds to correct port |
| Cascading startup failures   | A dependency failed; check the root resource first                             |

### Dashboard

| Problem                               | Solution                                                                  |
| ------------------------------------- | ------------------------------------------------------------------------- |
| Dashboard doesn't open                | Check terminal for URL; use `--dashboard-port` for fixed port             |
| No logs appearing                     | Service may not be writing to stdout/stderr; check console output         |
| No traces for non-.NET services       | Configure OpenTelemetry SDK in the service; see [Dashboard](dashboard.md) |
| Traces don't show cross-service calls | Propagate trace context headers (`traceparent`, `tracestate`)             |

### Build & configuration

| Problem                                   | Solution                                                            |
| ----------------------------------------- | ------------------------------------------------------------------- |
| "Project not found" for `AddProject<T>()` | Ensure `.csproj` is in the solution and referenced by AppHost       |
| Package version conflicts                 | Pin all Aspire packages to the same version                         |
| AppHost won't build                       | Check `Aspire.AppHost.Sdk` is in the project; run `dotnet restore`  |
| `aspire run` build error                  | Fix the build error first; `aspire run` requires a successful build |

### Deployment

| Problem                                  | Solution                                                             |
| ---------------------------------------- | -------------------------------------------------------------------- |
| `aspire publish` fails                   | Check publisher package is installed (e.g., `Aspire.Hosting.Docker`) |
| Generated Bicep has errors               | Check for unsupported resource configurations                        |
| Container image push fails               | Verify registry credentials and permissions                          |
| Missing connection strings in deployment | Check generated ConfigMaps/Secrets match resource names              |

---

## Debugging strategies

### 1. Check the dashboard first

The dashboard shows resource state, logs, traces, and metrics. Start here for any issue.

### 2. Check environment variables

In the dashboard, click a resource to see all injected environment variables. Verify connection strings and service URLs are correct.

### 3. Read console logs

Dashboard → Console Logs → filter by the failing resource. Raw stdout/stderr often contains the root cause.

### 4. Check the DAG

If services fail to start, check the dependency order. A failed dependency blocks all downstream resources.

### 5. Use MCP for AI-assisted debugging

If MCP is configured (see [MCP Server](mcp-server.md)), ask your AI assistant:

- "What resources are failing?"
- "Show me the logs for [service]"
- "What traces show errors?"

### 6. Isolate the problem

Run just the failing resource by commenting out others in the AppHost. This narrows whether the issue is the resource itself or a dependency.

---

## Getting help

| Channel                 | URL                                            |
| ----------------------- | ---------------------------------------------- |
| GitHub Issues (runtime) | https://github.com/dotnet/aspire/issues        |
| GitHub Issues (docs)    | https://github.com/microsoft/aspire.dev/issues |
| Discord                 | https://aka.ms/aspire/discord                  |
| Stack Overflow          | Tag: `dotnet-aspire`                           |
| Reddit                  | https://www.reddit.com/r/aspiredotdev/         |
