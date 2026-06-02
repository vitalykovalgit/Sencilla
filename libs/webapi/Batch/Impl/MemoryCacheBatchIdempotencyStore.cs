namespace Sencilla.Web.Batch;

/// <summary>
/// Single-instance idempotency store backed by <see cref="IMemoryCache"/>. Suitable
/// for one-pod deployments; replace with a distributed (Redis) implementation when
/// running multiple instances.
/// </summary>
internal sealed class MemoryCacheBatchIdempotencyStore(IMemoryCache cache) : IBatchIdempotencyStore
{
    public Task<BatchResult?> GetAsync(string key, CancellationToken token = default)
        => Task.FromResult(cache.TryGetValue(CacheKey(key), out BatchResult? result) ? result : null);

    public Task SetAsync(string key, BatchResult result, TimeSpan ttl, CancellationToken token = default)
    {
        cache.Set(CacheKey(key), result, ttl);
        return Task.CompletedTask;
    }

    private static string CacheKey(string key) => $"batch:idempotency:{key}";
}
