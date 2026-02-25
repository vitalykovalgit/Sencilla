namespace Sencilla.Scheduler.Tests;

public class ScheduledTasksSchedulerTests
{
    [Fact]
    public void Name_ReturnsConstructorValue()
    {
        var scheduler = CreateScheduler("test-scheduler");

        Assert.Equal("test-scheduler", scheduler.Name);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsInitialTasks()
    {
        var task = new ScheduledTask(new ScheduledTaskOptions { Name = "task1", Cron = "*/5 * * * * *" });
        var scheduler = CreateScheduler("scheduler", task);

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);

        Assert.Single(tasks);
    }

    [Fact]
    public async Task GetTasksAsync_Empty_ReturnsEmpty()
    {
        var scheduler = CreateScheduler("scheduler");

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task AddTaskAsync_AddsTask()
    {
        var scheduler = CreateScheduler("scheduler");

        await scheduler.AddTaskAsync(new ScheduledTaskOptions { Name = "new-task", Cron = "*/5 * * * * *" });

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task AddTaskAsync_WithConfigure_AddsTask()
    {
        var scheduler = CreateScheduler("scheduler");

        await scheduler.AddTaskAsync<TestHandler>(options =>
        {
            options.Cron = "*/5 * * * * *";
        });

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task AddTaskAsync_WithNameAndConfigure_AddsTask()
    {
        var scheduler = CreateScheduler("scheduler");

        await scheduler.AddTaskAsync<TestHandler>("my-task", options =>
        {
            options.Cron = "*/10 * * * * *";
        });

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);
        var task = tasks.First();
        Assert.Equal("my-task", task.Options.Name);
    }

    [Fact]
    public async Task AddTaskAsync_SameName_ReplacesTask()
    {
        var scheduler = CreateScheduler("scheduler");

        await scheduler.AddTaskAsync(new ScheduledTaskOptions { Name = "task", Cron = "*/5 * * * * *" });
        await scheduler.AddTaskAsync(new ScheduledTaskOptions { Name = "task", Cron = "*/10 * * * * *" });

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task RemoveTaskAsync_ByName_RemovesTask()
    {
        var task = new ScheduledTask(new ScheduledTaskOptions { Name = "task1", Cron = "*/5 * * * * *" });
        var scheduler = CreateScheduler("scheduler", task);

        await scheduler.RemoveTaskAsync("task1");

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task RemoveTaskAsync_ByTask_RemovesTask()
    {
        var task = new ScheduledTask(new ScheduledTaskOptions { Name = "task1", Cron = "*/5 * * * * *" });
        var scheduler = CreateScheduler("scheduler", task);

        await scheduler.RemoveTaskAsync(task);

        var tasks = await scheduler.GetTasksAsync(CancellationToken.None);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task RemoveTaskAsync_NonExisting_DoesNotThrow()
    {
        var scheduler = CreateScheduler("scheduler");

        await scheduler.RemoveTaskAsync("non-existing");
    }

    [Fact]
    public async Task Schedule_CancellationStopsLoop()
    {
        var scheduler = CreateScheduler("scheduler");
        using var cts = new CancellationTokenSource(200);

        await scheduler.Schedule(cts.Token);
        // Should exit cleanly after cancellation
    }

    private static ScheduledTasksScheduler CreateScheduler(string name, params ScheduledTask[] tasks)
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var options = new SchedulerOptions { Name = name };
        return new ScheduledTasksScheduler(name, sp, options, tasks);
    }

    private class TestHandler : IScheduledTaskHandler
    {
        public Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
