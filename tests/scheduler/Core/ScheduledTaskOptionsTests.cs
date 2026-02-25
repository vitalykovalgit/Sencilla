namespace Sencilla.Scheduler.Tests;

public class ScheduledTaskOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new ScheduledTaskOptions { Name = "test" };

        Assert.Equal("test", options.Name);
        Assert.Null(options.Cron);
        Assert.Null(options.Every);
        Assert.False(options.RunImmediately);
        Assert.Null(options.RunAt);
        Assert.Null(options.RunIn);
        Assert.Null(options.DelayFor);
        Assert.Null(options.Repeat);
        Assert.Null(options.Retry);
        Assert.Null(options.RetryIn);
        Assert.Null(options.TimeZone);
        Assert.Null(options.From);
        Assert.Null(options.To);
        Assert.Null(options.RunDuring);
        Assert.Null(options.Data);
        Assert.False(options.RunInThread);
        Assert.False(options.RunInSynch);
        Assert.False(options.WaitUntilCompleted);
        Assert.Equal(1, options.InstanceCount);
        Assert.Null(options.Batch);
        Assert.Null(options.Tag);
    }

    [Theory]
    [InlineData("*/5 * * * * *")]
    [InlineData("0 */10 * * * *")]
    [InlineData("0 0 * * * *")]
    public void GetCronExpression_WithCron_ParsesCorrectly(string cron)
    {
        var options = new ScheduledTaskOptions { Name = "test", Cron = cron };

        var expression = options.GetCronExpression();

        Assert.NotNull(expression);
    }

    [Fact]
    public void GetCronExpression_CachesResult()
    {
        var options = new ScheduledTaskOptions { Name = "test", Cron = "*/5 * * * * *" };

        var expr1 = options.GetCronExpression();
        var expr2 = options.GetCronExpression();

        Assert.Same(expr1, expr2);
    }

    [Fact]
    public void GetNextOccurrence_ReturnsPositiveTimeSpan()
    {
        var options = new ScheduledTaskOptions { Name = "test", Cron = "*/5 * * * * *" };

        var next = options.GetNextOccurrence(DateTime.UtcNow);

        Assert.True(next > TimeSpan.Zero);
        Assert.True(next <= TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetNextOccurrence_WithEvery_Seconds()
    {
        var options = new ScheduledTaskOptions
        {
            Name = "test",
            Every = TimeSpan.FromSeconds(30)
        };

        var next = options.GetNextOccurrence(DateTime.UtcNow);

        Assert.True(next > TimeSpan.Zero);
        Assert.True(next <= TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Merge_OverridesCron()
    {
        var options = new ScheduledTaskOptions { Name = "test", Cron = "*/5 * * * * *" };
        var other = new ScheduledTaskOptions { Name = "other", Cron = "*/10 * * * * *" };

        options.Merge(other);

        Assert.Equal("*/10 * * * * *", options.Cron);
    }

    [Fact]
    public void Merge_OverridesRunAt()
    {
        var runAt = DateTime.UtcNow.AddHours(1);
        var options = new ScheduledTaskOptions { Name = "test" };
        var other = new ScheduledTaskOptions { Name = "other", RunAt = runAt };

        options.Merge(other);

        Assert.Equal(runAt, options.RunAt);
    }

    [Fact]
    public void Merge_PreservesOriginalWhenOtherIsEmpty()
    {
        var options = new ScheduledTaskOptions
        {
            Name = "test",
            Cron = "*/5 * * * * *",
            Data = "original"
        };
        var other = new ScheduledTaskOptions { Name = "other" };

        options.Merge(other);

        Assert.Equal("*/5 * * * * *", options.Cron);
        Assert.Equal("original", options.Data);
    }

    [Fact]
    public void Merge_OverridesMultipleProperties()
    {
        var options = new ScheduledTaskOptions { Name = "test" };
        var other = new ScheduledTaskOptions
        {
            Name = "other",
            RunIn = TimeSpan.FromMinutes(5),
            DelayFor = TimeSpan.FromSeconds(10),
            Repeat = 3,
            Retry = 2,
            TimeZone = "UTC",
            Data = "data",
            Batch = "batch1",
            RunInThread = true,
            InstanceCount = 4
        };

        options.Merge(other);

        Assert.Equal(TimeSpan.FromMinutes(5), options.RunIn);
        Assert.Equal(TimeSpan.FromSeconds(10), options.DelayFor);
        Assert.Equal((ulong)3, options.Repeat);
        Assert.Equal((ulong)2, options.Retry);
        Assert.Equal("UTC", options.TimeZone);
        Assert.Equal("data", options.Data);
        Assert.Equal("batch1", options.Batch);
        Assert.True(options.RunInThread);
        Assert.Equal(4, options.InstanceCount);
    }

    [Fact]
    public void Merge_ReturnsThis()
    {
        var options = new ScheduledTaskOptions { Name = "test" };
        var other = new ScheduledTaskOptions { Name = "other" };

        var result = options.Merge(other);

        Assert.Same(options, result);
    }
}
