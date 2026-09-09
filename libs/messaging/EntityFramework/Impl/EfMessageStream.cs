namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// One logical queue backed by rows in [Message].
///
/// <para><b>Read blocks.</b> The stream consumer loops on <c>Read</c> and skips a null result, so a
/// database stream that returned null on an empty queue would spin the CPU. This one waits on
/// <see cref="Notify"/> or the poll interval instead, and only returns when it has actually claimed
/// a row.</para>
///
/// <para><b>Claim.</b> Oldest claimable row first, flipped New → InProgress under the RowVersion
/// concurrency token; a losing racer moves on to the next row, so each row is claimed exactly once
/// no matter how many workers compete.</para>
///
/// <para><b>Recovery.</b> On first read the instance re-queues anything left InProgress under its
/// own name — a fresh process owns nothing in flight, which makes crash recovery immediate and
/// needs no claim cache. Rows abandoned by an instance that never comes back are re-queued by age,
/// deliberately NOT scoped to an owner: owner-scoped rescue can never rescue a dead peer.</para>
/// </summary>
[DisableInjection]
public class EfMessageStream(
    IServiceScopeFactory scopeFactory,
    StreamConfig streamConfig,
    EfMessagingOptions options,
    ILogger logger) : IMessageStream, IMessageStreamAck
{
    private const byte New = (byte)MessageState.New;
    private const byte InProgress = (byte)MessageState.InProgress;
    private const byte Failed = (byte)MessageState.Failed;
    private const byte Succeeded = (byte)MessageState.Succeeded;

    /// <summary>Released by a doorbell (or a local enqueue) to skip the poll wait.</summary>
    private readonly SemaphoreSlim Signal = new(0);

    private bool Reclaimed;
    private DateTime NextRetentionSweep = DateTime.MinValue;

    public string Name { get; } = streamConfig.Name ?? throw new ArgumentNullException(nameof(streamConfig));

    /// <summary>Wake a waiting reader immediately instead of letting it wait out the poll interval.</summary>
    public void Notify()
    {
        if (Signal.CurrentCount == 0)
            Signal.Release();
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<string?> Read(CancellationToken cancellationToken = default)
    {
        // The rows are the transport's own: claiming, rescuing and sweeping them is a system action, not
        // something the current user (a worker has none) must hold a grant for. Enqueue is the caller's.
        using var root = Access.Root();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Reclaimed)
            {
                await ReclaimOwnAsync(cancellationToken);
                Reclaimed = true;
            }

            await RescueStuckAsync(cancellationToken);
            await SweepRetentionAsync(cancellationToken);

            var claimed = await ClaimAsync(cancellationToken);
            if (claimed != null)
                return claimed;

            // Doorbell or poll interval, whichever comes first.
            await Signal.WaitAsync(TimeSpan.FromSeconds(options.PollIntervalSeconds), cancellationToken);
        }
    }

    public async Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default)
    {
        var json = await Read(cancellationToken);
        return json is null ? default : JsonSerializer.Deserialize<Message<T>>(json, MessageJson.Options);
    }

    private async Task<string?> ClaimAsync(CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository<QueueMessage, Guid>>();

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var now = DateTime.UtcNow;
            var item = await repository.Query.AsNoTracking()
                .Where(m => m.Stream == Name && m.State == New && (m.AvailableAt == null || m.AvailableAt <= now))
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefaultAsync(token);

            if (item == null)
                return null;

            try
            {
                item.State = InProgress;
                item.ProcessService = options.InstanceId;
                item.ProcessStartDate = now;
                await repository.Update(item, token);
                return MessageEnvelope.From(item);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogDebug("Message {MessageId} was claimed by another worker; trying the next one", item.Id);
                repository.Detach([item]);
            }
        }
    }

    // ── Acknowledgement ───────────────────────────────────────────────────────

    public async Task Ack(Guid messageId, CancellationToken cancellationToken = default)
    {
        using var root = Access.Root();
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository<QueueMessage, Guid>>();

        await repository.ExecuteUpdateAsync(messageId, s =>
        {
            s.SetProperty(m => m.State, Succeeded);
            s.SetProperty(m => m.ProcessedAt, DateTime.UtcNow);
        }, cancellationToken);
    }

    /// <summary>
    /// Acknowledge inside the handler's own transaction: the row flips to Succeeded through the
    /// caller's DbContext — <see cref="AppMessage"/> maps the same table there — so the handler's
    /// writes and the acknowledgement commit together. A crash after that commit cannot redeliver,
    /// the row is already terminal; a crash before it rolls both back and the retry starts clean.
    /// </summary>
    public async Task Ack(Guid messageId, IServiceProvider scopedProvider, CancellationToken cancellationToken = default)
    {
        using var root = Access.Root();
        var repository = scopedProvider.GetRequiredService<IUpdateRepository<AppMessage, Guid>>();

        await repository.ExecuteUpdateAsync(messageId, s =>
        {
            s.SetProperty(m => m.State, Succeeded);
            s.SetProperty(m => m.ProcessedAt, DateTime.UtcNow);
        }, cancellationToken);
    }

    public async Task<bool> Nack(Guid messageId, string error, bool retryable, CancellationToken cancellationToken = default)
    {
        using var root = Access.Root();
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository<QueueMessage, Guid>>();

        var attempts = await repository.Query.AsNoTracking()
            .Where(m => m.Id == messageId)
            .Select(m => (int?)m.Attempts)
            .FirstOrDefaultAsync(cancellationToken);

        if (attempts is null)
        {
            logger.LogWarning("Cannot nack message {MessageId} on stream {Stream}: row is gone", messageId, Name);
            return false;
        }

        var attempt = attempts.Value + 1;
        var (terminal, availableAt) = RetryPolicy.Next(attempt, retryable, options, DateTime.UtcNow);

        await repository.ExecuteUpdateAsync(messageId, s =>
        {
            s.SetProperty(m => m.Attempts, attempt);
            s.SetProperty(m => m.Error, error);
            s.SetProperty(m => m.State, terminal ? Failed : New);
            s.SetProperty(m => m.AvailableAt, availableAt);
            s.SetProperty(m => m.ProcessService, (string?)null);
            if (terminal)
                s.SetProperty(m => m.ProcessedAt, DateTime.UtcNow);
        }, cancellationToken);

        logger.LogWarning("Message {MessageId} on stream {Stream} attempt {Attempt} failed — {Outcome}",
            messageId, Name, attempt, terminal ? "marked Failed" : $"retry at {availableAt:O}");

        return terminal;
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Non-transactional enqueue in its own scope — the <see cref="IMessageStream"/> contract has no
    /// place to pass the caller's DbContext. Application code enqueues through
    /// <see cref="IMessageQueue"/> so the row commits with the caller's transaction.
    /// </summary>
    public async Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        if (message is null) return;

        using var scope = scopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
        await queue.Enqueue(message, Name, null, cancellationToken);

        Notify();
    }

    // ── Maintenance ───────────────────────────────────────────────────────────

    /// <summary>A fresh process owns nothing in flight, so its own InProgress rows are crash debris.</summary>
    private async Task ReclaimOwnAsync(CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository<QueueMessage, Guid>>();

        var reclaimed = await repository.Query
            .Where(m => m.Stream == Name && m.State == InProgress && m.ProcessService == options.InstanceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.State, New)
                .SetProperty(m => m.ProcessService, (string?)null), token);

        if (reclaimed > 0)
            logger.LogWarning("Re-queued {Count} message(s) left InProgress by a previous run of {Instance} on stream {Stream}",
                reclaimed, options.InstanceId, Name);
    }

    /// <summary>
    /// Age-based rescue for rows whose claiming instance never came back. Deliberately not scoped to
    /// an owner — that is the case owner-scoped recovery can never handle.
    /// </summary>
    private async Task RescueStuckAsync(CancellationToken token)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-options.StuckAfterMinutes);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository<QueueMessage, Guid>>();

        var rescued = await repository.Query
            .Where(m => m.Stream == Name && m.State == InProgress && m.ProcessStartDate < cutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.State, New)
                .SetProperty(m => m.ProcessService, (string?)null), token);

        if (rescued > 0)
            logger.LogWarning("Rescued {Count} message(s) stuck InProgress for over {Minutes}m on stream {Stream}",
                rescued, options.StuckAfterMinutes, Name);
    }

    private async Task SweepRetentionAsync(CancellationToken token)
    {
        if (options.RetentionDays <= 0 || DateTime.UtcNow < NextRetentionSweep)
            return;

        NextRetentionSweep = DateTime.UtcNow.AddMinutes(options.RetentionSweepMinutes);
        var cutoff = DateTime.UtcNow.AddDays(-options.RetentionDays);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository<QueueMessage, Guid>>();

        // Terminal rows only: a Pending or stuck row must stay visible however old it is.
        var deleted = await repository.Query
            .Where(m => m.Stream == Name && (m.State == Succeeded || m.State == Failed) && m.CreatedAt < cutoff)
            .ExecuteDeleteAsync(token);

        if (deleted > 0)
            logger.LogInformation("Retention deleted {Count} terminal message(s) older than {Days}d on stream {Stream}",
                deleted, options.RetentionDays, Name);
    }
}
