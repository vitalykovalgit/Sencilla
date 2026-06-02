namespace Sencilla.Web.Batch;

/// <summary>
/// Result of a single batch step, echoing its identity plus outcome.
/// </summary>
public sealed class BatchStepResult
{
    public string Ref { get; set; } = "";
    public string Op { get; set; } = "";
    public string Entity { get; set; } = "";

    /// <summary>HTTP-style status: 201 create, 200 success, 4xx/5xx failure, 0 when skipped.</summary>
    public int Status { get; set; }

    /// <summary>Returned entity (writes / getById), array (getAll) or scalar (getCount). Null on failure.</summary>
    public object? Data { get; set; }

    /// <summary>Error message when the step failed.</summary>
    public string? Error { get; set; }

    /// <summary>True when an earlier write failed and this write never ran.</summary>
    public bool Skipped { get; set; }
}
