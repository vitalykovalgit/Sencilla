namespace Sencilla.Web.Batch;

/// <summary>
/// Persists batch results keyed by idempotency key so a repeated submission returns
/// the original outcome instead of re-executing. Default implementation is
/// single-instance (<see cref="MemoryCacheBatchIdempotencyStore"/>); register a
/// distributed (Redis) implementation for multi-pod deployments.
/// </summary>
public interface IBatchIdempotencyStore
{
    Task<BatchResult?> GetAsync(string key, CancellationToken token = default);
    Task SetAsync(string key, BatchResult result, TimeSpan ttl, CancellationToken token = default);
}
