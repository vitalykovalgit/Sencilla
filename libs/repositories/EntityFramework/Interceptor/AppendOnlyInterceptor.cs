namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Enforces the <see cref="IEntityAppendOnly"/> contract at save time.
///
/// Plain append-only entities may be inserted but never updated or deleted. For the temporal variant
/// <see cref="IEntityAppendOnlyTrack"/> the single exception is closing a row: a Modified entry is allowed
/// only when the one changed property is <see cref="IEntityAppendOnlyTrack.ActiveTo"/> and it was previously
/// null (open → closed). Everything else — any other field change, re-closing, or delete — is rejected.
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

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IEntityAppendOnly) continue;

            if (entry.State == EntityState.Deleted)
                throw AppendOnlyViolation(entry.Entity, "deleted");

            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is IEntityAppendOnlyTrack && IsOnlyActiveToClose(entry))
                    continue;

                throw AppendOnlyViolation(entry.Entity, "modified");
            }
        }
    }

    // A close is: exactly one modified property, it is ActiveTo, and it was previously null.
    static bool IsOnlyActiveToClose(EntityEntry entry)
    {
        var modified = entry.Properties.Where(p => p.IsModified).ToList();
        if (modified.Count != 1) return false;

        var property = modified[0];
        return property.Metadata.Name == nameof(IEntityAppendOnlyTrack.ActiveTo)
            && property.OriginalValue == null;
    }

    static InvalidOperationException AppendOnlyViolation(object entity, string action) => new(
        $"Entity '{entity.GetType().Name}' is append-only ({nameof(IEntityAppendOnly)}) and cannot be {action}. " +
        $"Append a new version instead (temporal entities may only have ActiveTo closed once).");
}
