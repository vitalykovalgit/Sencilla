namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Enforces the <see cref="IEntityAppendOnly"/> contract at save time.
///
/// Plain append-only entities may be inserted but never updated or deleted. The temporal variant
/// <see cref="IEntityAppendOnlyTrack"/> permits a few mutations that only ever touch <b>future</b> time and
/// so cannot rewrite anything already booked:
/// <list type="bullet">
///   <item>closing an open version once (<c>ActiveTo</c>: null → a date, now or a future cut-point);</item>
///   <item>editing or deleting a <b>scheduled</b> version — one whose stored <c>ActiveFrom</c> is still in
///         the future (a not-yet-active row that no booked record can reference);</item>
///   <item>reopening/extending a version whose <c>ActiveTo</c> is still in the future (<c>value</c> → null
///         or a later instant) — used when cancelling a scheduled change.</item>
/// </list>
/// Everything else — any content change to an active/past version, re-closing into the past, or deleting an
/// active version — is rejected.
///
/// Note: set-based <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> bypass the change tracker and are not
/// intercepted; append-only entities reach the DB only through the append/supersede repository.
/// </summary>
public sealed class AppendOnlyInterceptor : SaveChangesInterceptor
{
    public static readonly AppendOnlyInterceptor Instance = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    static void Guard(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IEntityAppendOnly) continue;

            var tracked = entry.Entity is IEntityAppendOnlyTrack;

            if (entry.State == EntityState.Deleted)
            {
                // A scheduled (not-yet-active) version may be cancelled — its whole interval is in the
                // future, so no booked record ever referenced it.
                if (tracked && WasScheduled(entry, now))
                    continue;

                throw AppendOnlyViolation(entry.Entity, "deleted");
            }

            if (entry.State == EntityState.Modified)
            {
                if (tracked)
                {
                    // Editing a scheduled version is safe, provided it stays in the future.
                    if (WasScheduled(entry, now) && StaysScheduled(entry, now))
                        continue;

                    // Closing an open version once (ActiveTo: null → a date), to now or a future cut-point.
                    if (IsOnlyActiveToClose(entry))
                        continue;

                    // Reopening / extending a version whose close-point is still in the future.
                    if (IsOnlyActiveToFutureReopen(entry, now))
                        continue;
                }

                throw AppendOnlyViolation(entry.Entity, "modified");
            }
        }
    }

    // The row's stored (pre-edit) interval starts in the future: it is scheduled, not yet active.
    static bool WasScheduled(EntityEntry entry, DateTime now)
        => entry.Property(nameof(IEntityAppendOnlyTrack.ActiveFrom)).OriginalValue is DateTime from && from > now;

    // After the edit the row still starts in the future (an edit must not pull a scheduled row into the past).
    static bool StaysScheduled(EntityEntry entry, DateTime now)
        => entry.Property(nameof(IEntityAppendOnlyTrack.ActiveFrom)).CurrentValue is DateTime from && from > now;

    // A close is: exactly one modified property, it is ActiveTo, and it was previously null.
    static bool IsOnlyActiveToClose(EntityEntry entry)
    {
        var modified = entry.Properties.Where(p => p.IsModified).ToList();
        if (modified.Count != 1) return false;

        var property = modified[0];
        return property.Metadata.Name == nameof(IEntityAppendOnlyTrack.ActiveTo)
            && property.OriginalValue == null;
    }

    // A reopen/extend is: exactly one modified property, it is ActiveTo, its previous value is a future
    // instant, and its new value is null (reopen) or a later instant (extend). Only touches future time.
    static bool IsOnlyActiveToFutureReopen(EntityEntry entry, DateTime now)
    {
        var modified = entry.Properties.Where(p => p.IsModified).ToList();
        if (modified.Count != 1) return false;

        var property = modified[0];
        if (property.Metadata.Name != nameof(IEntityAppendOnlyTrack.ActiveTo)) return false;
        if (property.OriginalValue is not DateTime oldTo || oldTo <= now) return false;

        return property.CurrentValue is null || (property.CurrentValue is DateTime newTo && newTo > now);
    }

    static InvalidOperationException AppendOnlyViolation(object entity, string action) => new(
        $"Entity '{entity.GetType().Name}' is append-only ({nameof(IEntityAppendOnly)}) and cannot be {action}. " +
        $"Append a new version instead (temporal entities may close ActiveTo once and may edit/cancel a not-yet-active version).");
}
