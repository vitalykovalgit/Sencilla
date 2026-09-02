namespace Sencilla.Messaging.EntityFramework.Tests;

/// <summary>
/// What a failed attempt does to the row. The backoff is the fix for burning an entire attempt
/// budget in milliseconds on a transient outage.
/// </summary>
public class RetryPolicyTests
{
    private static readonly DateTime Now = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

    private static EfMessagingOptions Options(int maxAttempts = 3, bool exponential = true) => new()
    {
        MaxAttempts = maxAttempts,
        RetryBaseDelaySeconds = 2,
        UseExponentialBackoff = exponential,
    };

    [Theory]
    [InlineData(1, 2)]   // base × 2^0
    [InlineData(2, 4)]   // base × 2^1
    public void Next_RetryableBeforeTheLastAttempt_SchedulesExponentialBackoff(int attempt, int expectedSeconds)
    {
        var (terminal, availableAt) = RetryPolicy.Next(attempt, retryable: true, Options(), Now);

        Assert.False(terminal);
        Assert.Equal(Now.AddSeconds(expectedSeconds), availableAt);
    }

    [Fact]
    public void Next_ConstantBackoff_UsesTheBaseDelayEveryTime()
    {
        var (_, availableAt) = RetryPolicy.Next(3, retryable: true, Options(maxAttempts: 10, exponential: false), Now);

        Assert.Equal(Now.AddSeconds(2), availableAt);
    }

    [Fact]
    public void Next_AttemptsExhausted_IsTerminalAndNotClaimableAgain()
    {
        var (terminal, availableAt) = RetryPolicy.Next(3, retryable: true, Options(), Now);

        Assert.True(terminal);
        Assert.Null(availableAt);
    }

    [Fact]
    public void Next_NonRetryable_IsTerminalOnTheFirstAttempt()
    {
        // An unresolvable payload type or a missing handler can never succeed — spending the whole
        // attempt budget on it would only delay the failure becoming visible.
        var (terminal, availableAt) = RetryPolicy.Next(1, retryable: false, Options(), Now);

        Assert.True(terminal);
        Assert.Null(availableAt);
    }
}
