namespace Sencilla.Messaging.EntityFramework;

/// <summary>What a failed attempt does to the row: give up, or come back later.</summary>
public static class RetryPolicy
{
    /// <summary>
    /// Decides the outcome of attempt <paramref name="attempt"/> (1-based).
    /// A non-retryable failure goes terminal immediately rather than burning the attempt budget on
    /// something that can never succeed.
    /// </summary>
    public static (bool Terminal, DateTime? AvailableAt) Next(int attempt, bool retryable, EfMessagingOptions options, DateTime now)
    {
        if (!retryable || attempt >= options.MaxAttempts)
            return (true, null);

        var seconds = options.UseExponentialBackoff
            ? options.RetryBaseDelaySeconds * Math.Pow(2, attempt - 1)
            : options.RetryBaseDelaySeconds;

        return (false, now.AddSeconds(seconds));
    }
}
