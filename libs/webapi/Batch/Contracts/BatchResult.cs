namespace Sencilla.Web.Batch;

/// <summary>
/// Outcome of a batch. <see cref="ValidationErrors"/> non-empty means nothing ran
/// (pre-flight rejection → HTTP 400). Otherwise the batch executed and per-step
/// detail is in <see cref="Steps"/>.
/// </summary>
public sealed class BatchResult
{
    /// <summary>True when every step succeeded (and committed, if transactional).</summary>
    public bool Success { get; set; }

    /// <summary>True when a write failed and the transaction was rolled back.</summary>
    public bool RolledBack { get; set; }

    /// <summary>Pre-flight validation errors. When present, no step executed.</summary>
    public List<string>? ValidationErrors { get; set; }

    /// <summary>Per-step results, in the original declared order.</summary>
    public List<BatchStepResult> Steps { get; set; } = [];

    /// <summary>Whether this result was served from the idempotency cache.</summary>
    public bool FromCache { get; set; }

    public static BatchResult Invalid(List<string> errors) => new()
    {
        Success = false,
        ValidationErrors = errors,
    };
}
