namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// Worker-side knobs for the durable database transport (bound from the "Messaging" section).
/// App-domain policies (grace periods, business schedules) belong to the consuming app's options.
/// </summary>
public class EfMessagingOptions
{
    /// <summary>
    /// Claim identity written to ProcessService. MUST be unique per process — two replicas sharing
    /// it means one reclaims rows the other is actively processing. Defaults to the machine/pod
    /// name, which is unique per pod under Kubernetes; only override it when that is not true.
    /// </summary>
    public string InstanceId { get; set; } = Environment.MachineName;

    /// <summary>Sleep between claim attempts when the stream is empty; a doorbell shortcuts it.</summary>
    public int PollIntervalSeconds { get; set; } = 20;

    /// <summary>Delivery attempts before a message goes terminally Failed.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Base delay before a retryable failure becomes claimable again.</summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;

    /// <summary>Exponential (base × 2^(attempts-1)) rather than constant retry spacing.</summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Rows left InProgress this long are presumed orphaned and re-queued — regardless of which
    /// instance claimed them, because the instance that never comes back is the whole point. Must
    /// exceed the longest legitimate handler runtime.
    /// </summary>
    public int StuckAfterMinutes { get; set; } = 30;

    /// <summary>Terminal rows (Succeeded/Failed) older than this are deleted. 0 disables the sweep.</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>How often the retention sweep runs.</summary>
    public int RetentionSweepMinutes { get; set; } = 60;
}
