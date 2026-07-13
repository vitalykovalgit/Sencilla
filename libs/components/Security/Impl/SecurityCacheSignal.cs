using Microsoft.Extensions.Primitives;

namespace Sencilla.Component.Security;

/// <summary>
/// Process-wide invalidation signal for the security caches (permission matrix,
/// per-user roles). Deletes by id carry no entity payload, so invalidation is
/// signal-wide rather than per-entry: cache entries subscribe via <see cref="Token"/>
/// and all expire together on <see cref="Invalidate"/>. Per-process only — the short
/// TTL on each entry remains the backstop for multi-node deployments (and for the
/// pre-commit eviction race, where a concurrent request could re-cache state read
/// just before a change committed).
/// </summary>
[SingletonLifetime]
public class SecurityCacheSignal
{
    private CancellationTokenSource _cts = new();

    public IChangeToken Token => new CancellationChangeToken(_cts.Token);

    public void Invalidate()
    {
        var previous = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }
}
