namespace Sencilla.Web.Batch;

/// <summary>
/// Tunables for the batch API, configured via <c>AddSencillaBatch</c>.
/// </summary>
public sealed class BatchOptions
{
    /// <summary>Maximum steps accepted in one batch. Pre-flight rejects larger batches.</summary>
    public int MaxSteps { get; set; } = 50;

    /// <summary>How long an idempotent result is retained.</summary>
    public TimeSpan IdempotencyTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>JSON options used to (de)serialize step bodies, filters and results.</summary>
    public JsonSerializerOptions Json { get; set; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };
}
