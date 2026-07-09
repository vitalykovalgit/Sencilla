namespace Sencilla.Component.Audit;

/// <summary>
/// Records inserts/updates/deletes of an <see cref="IEntityAuditable"/> entity into the audit log. Registered
/// per auditable type by <see cref="AuditRegistrator"/> (the TrackStampHandler pattern). Runs on the write
/// pipeline, inside the ambient transaction, so audit rows are atomic with the change.
///
/// Event choice is driven by data availability: <b>Insert</b> on <see cref="EntityCreatedEvent{T}"/> (Ids are
/// known post-save); <b>Update</b> on <see cref="EntityUpdatingEvent{T}"/> (its DbEntities pre-image, enumerated
/// before the write, is the OLD row; Entities is the incoming NEW row); <b>Delete</b> on
/// <see cref="EntityDeletingEvent{T}"/> (its Entities is the DB query of rows about to be removed).
/// </summary>
public class AuditHandler<T>
    : IEventHandlerBase<EntityCreatedEvent<T>>
    , IEventHandlerBase<EntityUpdatingEvent<T>>
    , IEventHandlerBase<EntityDeletingEvent<T>>
    where T : class
{
    // repo (ICreateRepository<Audit, long>) is injected per handler call; Create runs inside the ambient
    // transaction, so audit rows commit or roll back atomically with the audited change.
    public Task HandleAsync(EntityCreatedEvent<T> @event, IAuditContext ctx, ICreateRepository<Audit, long> repo, CancellationToken token)
    {
        var rows = (@event.Entities?.ToList() ?? []).Select(e => AuditFactory.Insert(e, ctx)).ToList();
        return rows.Count == 0 ? Task.CompletedTask : repo.Create(rows, token);
    }

    public Task HandleAsync(EntityUpdatingEvent<T> @event, IAuditContext ctx, ICreateRepository<Audit, long> repo, CancellationToken token)
    {
        var news = @event.Entities?.ToList() ?? [];
        if (news.Count == 0)
            return Task.CompletedTask;

        var olds = (@event.DbEntities?.ToList() ?? []).ToDictionary(AuditFactory.IdOf);
        var rows = new List<Audit>();
        foreach (var n in news)
            if (olds.TryGetValue(AuditFactory.IdOf(n), out var old) && AuditFactory.Update(old, n, ctx) is { } row)
                rows.Add(row);

        return rows.Count == 0 ? Task.CompletedTask : repo.Create(rows, token);
    }

    public Task HandleAsync(EntityDeletingEvent<T> @event, IAuditContext ctx, ICreateRepository<Audit, long> repo, CancellationToken token)
    {
        var rows = (@event.Entities?.ToList() ?? []).Select(e => AuditFactory.Delete(e, ctx)).ToList();
        return rows.Count == 0 ? Task.CompletedTask : repo.Create(rows, token);
    }
}
