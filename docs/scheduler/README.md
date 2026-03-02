# Scheduler

[Home](../../README.md) / [Docs](../index.md) / Scheduler

**NuGet:** `Sencilla.Scheduler`
**Namespace:** `Sencilla.Scheduler`
**Source:** `libs/scheduler/Core/`

> **Status:** Documentation in progress. Core interface reference is complete.

Sencilla Scheduler runs background tasks on a **cron schedule** or **interval**. It integrates with ASP.NET Core hosted services and optionally persists task state to a database via `Sencilla.Scheduler.EntityFramework`.

---

## Packages

| Package | Purpose |
| ------- | ------- |
| `Sencilla.Scheduler` | Core scheduler, interfaces, hosted service |
| `Sencilla.Scheduler.EntityFramework` | EF Core persistence for task instances and history |
| `Sencilla.Scheduler.SourceGenerator` | Compile-time task registration code generation |

---

## Installation

```bash
dotnet add package Sencilla.Scheduler

# Optional: persist task state to database
dotnet add package Sencilla.Scheduler.EntityFramework
```

---

## Core Interfaces

### `IScheduledTaskHandler`

Implement this for every background task:

```csharp
public interface IScheduledTaskHandler
{
    Task HandleAsync(ScheduledTask task, CancellationToken token);
}
```

### `IScheduledTasksScheduler`

The scheduler itself. Inject to add/remove tasks at runtime:

```csharp
public interface IScheduledTasksScheduler
{
    string Name { get; }
    Task<IEnumerable<ScheduledTask>> GetTasksAsync(CancellationToken token);
    Task AddTaskAsync(ScheduledTaskOptions options, CancellationToken token = default);
    Task AddTaskAsync<T>(Action<ScheduledTaskOptions> configure)
        where T : IScheduledTaskHandler;
    Task AddTaskAsync<T>(string name, Action<ScheduledTaskOptions> configure)
        where T : IScheduledTaskHandler;
    Task RemoveTaskAsync(string taskName);
    Task RemoveTaskAsync(ScheduledTask task);
    Task Schedule(CancellationToken token);
}
```

---

## `ScheduledTask` Entity

```csharp
public class ScheduledTask
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CronExpression { get; set; }    // e.g. "0 * * * *" = hourly
    public int Interval { get; set; }              // Interval value
    public ScheduledTaskUnit Unit { get; set; }    // Minutes, Hours, Days
    public ScheduledTaskStatus Status { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
}

public enum ScheduledTaskUnit { Minutes, Hours, Days }
public enum ScheduledTaskStatus { Pending, Running, Completed, Failed }
```

---

## Attributes

### `[ScheduleTask]`

Decorate a handler class to register it automatically:

```csharp
[ScheduleTask(Name = "CleanupExpired", CronExpression = "0 2 * * *")]
[Implement]
public class CleanupExpiredOrdersTask : IScheduledTaskHandler
{
    private readonly IDeleteRepository<Order, int> _orders;

    public CleanupExpiredOrdersTask(IDeleteRepository<Order, int> orders)
        => _orders = orders;

    public async Task HandleAsync(ScheduledTask task, CancellationToken token)
    {
        // Runs daily at 02:00
        var filter = new Filter();
        filter.AddProperty("ExpiresAt", FilterPropertyType.Less, DateTime.UtcNow);
        var expired = await _orders.GetAll(filter);
        await _orders.Delete(expired, token);
    }
}
```

### `[ScheduleTasks]`

Register multiple schedules for one handler.

### `[Task]`

Metadata annotation for task display name and description.

---

## Quick Start

### 1. Define a task handler

```csharp
[ScheduleTask(Name = "SendDailyReport", CronExpression = "0 8 * * *")]
[Implement]
public class DailyReportTask : IScheduledTaskHandler
{
    private readonly IReportService _reports;
    private readonly IEmailService _email;

    public DailyReportTask(IReportService reports, IEmailService email)
    {
        _reports = reports;
        _email = email;
    }

    public async Task HandleAsync(ScheduledTask task, CancellationToken token)
    {
        var report = await _reports.GenerateDailyAsync(token);
        await _email.SendAsync("admin@company.com", "Daily Report", report, token);
    }
}
```

### 2. Register the scheduler

```csharp
// Program.cs
builder.Services.AddSencillaScheduler();
builder.Services.AddSencilla(builder.Configuration);
```

### 3. Add tasks at runtime (optional)

```csharp
public class SchedulerStartup(IScheduledTasksScheduler scheduler) : IHostedService
{
    public Task StartAsync(CancellationToken token)
    {
        return scheduler.AddTaskAsync<SyncInventoryTask>("SyncInventory", options =>
        {
            options.CronExpression = "*/15 * * * *";  // every 15 minutes
        });
    }

    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
```

---

## Cron Expression Reference

| Expression | Meaning |
| ---------- | ------- |
| `* * * * *` | Every minute |
| `0 * * * *` | Every hour |
| `0 8 * * *` | Daily at 08:00 |
| `0 8 * * 1` | Every Monday at 08:00 |
| `0 0 1 * *` | First day of every month |
| `*/15 * * * *` | Every 15 minutes |
| `0 9-17 * * 1-5` | Every hour, 9–17, Mon–Fri |

Sencilla Scheduler uses [Cronos](https://github.com/HangfireIO/Cronos) for cron parsing.

---

## Persistence with Entity Framework

```bash
dotnet add package Sencilla.Scheduler.EntityFramework
```

```csharp
builder.Services.AddSencillaScheduler(options =>
{
    options.UseEntityFramework<AppDbContext>();
});
```

With EF Core persistence, task history and execution records are stored in the database:

| Table | Description |
| ----- | ----------- |
| `ScheduledTasks` | Task definitions |
| `ScheduledTaskInstances` | Per-run instances |
| `ScheduledTaskExecutions` | Execution history (start, end, status) |

---

## Coming Soon

- Multiple scheduler configurations
- Exception handling strategies
- Logging integration
- Batch task processing
- Distributed lock for multi-instance deployments

---

## See Also

- [Messaging](../messaging/README.md) — `Sencilla.Messaging.Scheduler` for message-driven scheduling
- [Dependency Injection](../core/dependency-injection.md) — `[Implement]` for task registration

---

[Home](../../README.md) / [Docs](../index.md) / **Scheduler**
