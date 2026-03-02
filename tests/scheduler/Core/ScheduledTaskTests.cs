using System.Reflection;

namespace Sencilla.Scheduler.Tests;

public class ScheduledTaskTests
{
    [Fact]
    public void Constructor_SetsOptions()
    {
        var options = new ScheduledTaskOptions { Name = "test-task" };
        var task = new ScheduledTask(options);

        Assert.Same(options, task.Options);
        Assert.NotEqual(Guid.Empty, task.Id);
    }

    [Fact]
    public void Handlers_InitiallyNull()
    {
        var task = CreateTask("test");

        Assert.Null(task.Handlers);
    }

    [Fact]
    public void AddHandler_Generic_AddsHandler()
    {
        var task = CreateTask("test");

        task.AddHandler<TestHandler>();

        Assert.NotNull(task.Handlers);
        Assert.Single(task.Handlers!);
        Assert.Equal(typeof(TestHandler), task.Handlers![0].HandlerType);
    }

    [Fact]
    public void AddHandler_DuplicateType_DoesNotAddTwice()
    {
        var task = CreateTask("test");

        task.AddHandler<TestHandler>();
        task.AddHandler<TestHandler>();

        Assert.Single(task.Handlers!);
    }

    [Fact]
    public void AddHandler_DifferentTypes_AddsBoth()
    {
        var task = CreateTask("test");

        task.AddHandler<TestHandler>();
        task.AddHandler<AnotherHandler>();

        Assert.Equal(2, task.Handlers!.Count);
    }

    [Fact]
    public void AddHandler_WithMethod_SetsMethod()
    {
        var task = CreateTask("test");
        var method = typeof(TestHandler).GetMethod(nameof(TestHandler.HandleAsync))!;

        task.AddHandler(typeof(TestHandler), method);

        Assert.NotNull(task.Handlers![0].Method);
    }

    [Fact]
    public void AddHandler_ReturnsSelf_ForFluent()
    {
        var task = CreateTask("test");

        var result = task.AddHandler<TestHandler>();

        Assert.Same(task, result);
    }

    [Fact]
    public void SetName_UpdatesOptionsName()
    {
        var task = CreateTask("original");

        task.SetName("updated");

        Assert.Equal("updated", task.Options.Name);
    }

    [Fact]
    public void SetName_ReturnsSelf()
    {
        var task = CreateTask("test");

        var result = task.SetName("new-name");

        Assert.Same(task, result);
    }

    [Fact]
    public void GetNextOccurrence_DelegatesToOptions()
    {
        var task = CreateTask("test", "*/5 * * * * *");

        var next = task.GetNextOccurrence(DateTime.UtcNow);

        Assert.True(next > TimeSpan.Zero);
        Assert.True(next <= TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Merge_CombinesHandlers()
    {
        var task1 = CreateTask("task1");
        task1.AddHandler<TestHandler>();

        var task2 = CreateTask("task2");
        task2.AddHandler<AnotherHandler>();

        task1.Merge(task2);

        Assert.Equal(2, task1.Handlers!.Count);
    }

    [Fact]
    public void Merge_DoesNotDuplicateHandlers()
    {
        var task1 = CreateTask("task1");
        task1.AddHandler<TestHandler>();

        var task2 = CreateTask("task2");
        task2.AddHandler<TestHandler>();

        task1.Merge(task2);

        Assert.Single(task1.Handlers!);
    }

    [Fact]
    public void Merge_MergesOptions()
    {
        var task1 = CreateTask("task1");
        var task2 = CreateTask("task2", "*/10 * * * * *");

        task1.Merge(task2);

        Assert.Equal("*/10 * * * * *", task1.Options.Cron);
    }

    [Fact]
    public void Merge_ReturnsSelf()
    {
        var task1 = CreateTask("task1");
        var task2 = CreateTask("task2");

        var result = task1.Merge(task2);

        Assert.Same(task1, result);
    }

    [Fact]
    public void UniqueIds_AreGenerated()
    {
        var task1 = CreateTask("task1");
        var task2 = CreateTask("task2");

        Assert.NotEqual(task1.Id, task2.Id);
    }

    private static ScheduledTask CreateTask(string name, string? cron = null)
    {
        return new ScheduledTask(new ScheduledTaskOptions { Name = name, Cron = cron });
    }

    private class TestHandler : IScheduledTaskHandler
    {
        public Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherHandler : IScheduledTaskHandler
    {
        public Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
