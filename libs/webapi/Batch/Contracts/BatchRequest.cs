namespace Sencilla.Web.Batch;

/// <summary>
/// A batch of dependent CRUD/GET steps submitted to <c>POST /api/v1/batch</c>.
/// </summary>
public sealed class BatchRequest
{
    /// <summary>Schema version. Only <c>v1</c> is accepted.</summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// When true (default) all write steps run inside a single DB transaction
    /// (all-or-nothing). When false, writes are not transactional.
    /// </summary>
    public bool Transactional { get; set; } = true;

    /// <summary>
    /// Optional. When present, the result is cached and a repeat with the same key
    /// returns the cached result without re-executing. Required by convention for
    /// background jobs.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Ordered list of steps. Capped by <see cref="BatchOptions.MaxSteps"/>.</summary>
    public List<BatchStep> Steps { get; set; } = [];
}
