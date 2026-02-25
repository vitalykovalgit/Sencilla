namespace Sencilla.Scheduler.Tests;

public class ScheduledTasksRunnerTests
{
    [Fact]
    public async Task ExecuteTasks_NoHandlers_CompletesImmediately()
    {
        var runner = CreateRunner();
        var task = new ScheduledTask(new ScheduledTaskOptions { Name = "test", Cron = "*/5 * * * * *" });

        await runner.ExecuteTasks([task], CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteTasks_WithHandler_ExecutesHandler()
    {
        var handler = new CountingHandler();
        var runner = CreateRunner(handler);
        var task = CreateTaskWithHandler<CountingHandler>("test");

        await runner.ExecuteTasks([task], CancellationToken.None);

        // Give a moment for Task.Run to complete
        await Task.Delay(100);
        Assert.Equal(1, handler.ExecutionCount);
    }

    [Fact]
    public async Task ExecuteTasks_MultipleTasks_ExecutesAll()
    {
        var handler = new CountingHandler();
        var runner = CreateRunner(handler);
        var task1 = CreateTaskWithHandler<CountingHandler>("task1");
        var task2 = CreateTaskWithHandler<CountingHandler>("task2");

        await runner.ExecuteTasks([task1, task2], CancellationToken.None);

        await Task.Delay(100);
        Assert.Equal(2, handler.ExecutionCount);
    }

    [Fact]
    public async Task ExecuteTasks_EmptyList_CompletesImmediately()
    {
        var runner = CreateRunner();

        await runner.ExecuteTasks([], CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteTasks_MultipleInstanceCount_RunsMultipleTimes()
    {
        var handler = new CountingHandler();
        var runner = CreateRunner(handler);
        var task = new ScheduledTask(new ScheduledTaskOptions
        {
            Name = "test",
            Cron = "*/5 * * * * *",
            InstanceCount = 3
        });
        task.AddHandler<CountingHandler>();

        await runner.ExecuteTasks([task], CancellationToken.None);

        await Task.Delay(200);
        Assert.Equal(3, handler.ExecutionCount);
    }

    [Fact]
    public async Task ExecuteTasks_WithCancellation_StopsExecution()
    {
        var handler = new SlowHandler();
        var runner = CreateRunner(handler);
        var task = CreateTaskWithHandler<SlowHandler>("test");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await runner.ExecuteTasks([task], cts.Token);
    }

    private static ScheduledTasksRunner CreateRunner(params object[] handlers)
    {
        var services = new ServiceCollection();
        foreach (var handler in handlers)
        {
            services.AddSingleton(handler.GetType(), handler);
        }
        var sp = services.BuildServiceProvider();
        return new ScheduledTasksRunner(sp);
    }

    private static ScheduledTask CreateTaskWithHandler<T>(string name) where T : IScheduledTaskHandler
    {
        var task = new ScheduledTask(new ScheduledTaskOptions { Name = name, Cron = "*/5 * * * * *" });
        task.AddHandler<T>();
        return task;
    }

    private class CountingHandler : IScheduledTaskHandler
    {
        public int ExecutionCount;

        public Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref ExecutionCount);
            return Task.CompletedTask;
        }
    }

    private class SlowHandler : IScheduledTaskHandler
    {
        public async Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);
        }
    }
}
