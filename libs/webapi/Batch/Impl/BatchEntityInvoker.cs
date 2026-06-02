namespace Sencilla.Web.Batch;

/// <summary>
/// Strongly-typed dispatcher for a single entity. All repository calls are compiled
/// (no per-call reflection); only constructing this closed generic is reflective.
/// </summary>
internal sealed class BatchEntityInvoker<TEntity, TKey> : IBatchEntityInvoker
    where TEntity : class, IEntity<TKey>
{
    public async Task<IDbTransaction> BeginTransactionAsync(IServiceProvider services, CancellationToken token)
    {
        var repo = services.GetService<IReadRepository<TEntity, TKey>>()
            ?? throw new BatchOperationNotSupportedException($"No read repository registered for '{typeof(TEntity).Name}'.");
        return await repo.BeginTransaction(token);
    }

    public async Task<BatchOpOutcome> InvokeAsync(
        BatchOp op,
        IServiceProvider services,
        JsonObject? body,
        JsonElement? id,
        IFilter? filter,
        JsonSerializerOptions json,
        CancellationToken token)
    {
        switch (op)
        {
            case BatchOp.Create:
            {
                var repo = Create(services);
                var created = await repo.Create(Entity(body, json), token);
                return new(201, created);
            }
            case BatchOp.Update:
            {
                var repo = Update(services);
                var updated = await repo.Update(Entity(body, json), token);
                return new(200, updated);
            }
            case BatchOp.Upsert:
            {
                var repo = Create(services);
                var upserted = await repo.UpsertAsync(Entity(body, json), x => x.Id!, token: token);
                return new(200, upserted);
            }
            case BatchOp.Remove:
            {
                var repo = Resolve<IRemoveRepository<TEntity, TKey>>(services, op);
                var removed = await repo.Remove(Entity(body, json), token);
                return new(200, removed);
            }
            case BatchOp.Hide:
            {
                var repo = Resolve<IHideRepository<TEntity, TKey>>(services, op);
                var hidden = await repo.Hide(Entity(body, json), token);
                return new(200, hidden);
            }
            case BatchOp.Show:
            {
                var repo = Resolve<IHideRepository<TEntity, TKey>>(services, op);
                var shown = await repo.Show(Entity(body, json), token);
                return new(200, shown);
            }
            case BatchOp.Delete:
            {
                var repo = Resolve<IDeleteRepository<TEntity, TKey>>(services, op);
                if (id is { } key)
                {
                    var count = await repo.Delete(Key(key, json), token);
                    return new(200, count);
                }
                var deleted = await repo.Delete(Entity(body, json), token);
                return new(200, deleted);
            }
            case BatchOp.GetById:
            {
                var repo = Read(services);
                var item = await repo.GetById(Key(id!.Value, json), token);
                return new(200, item);
            }
            case BatchOp.GetAll:
            {
                var repo = Read(services);
                var items = await repo.GetAll(filter, token);
                return new(200, items);
            }
            case BatchOp.GetCount:
            {
                var repo = Read(services);
                var count = await repo.GetCount(filter, token);
                return new(200, count);
            }
            default:
                throw new BatchOperationNotSupportedException($"Op '{op}' is not handled.");
        }
    }

    private static ICreateRepository<TEntity, TKey> Create(IServiceProvider sp) => Resolve<ICreateRepository<TEntity, TKey>>(sp, BatchOp.Create);
    private static IUpdateRepository<TEntity, TKey> Update(IServiceProvider sp) => Resolve<IUpdateRepository<TEntity, TKey>>(sp, BatchOp.Update);
    private static IReadRepository<TEntity, TKey> Read(IServiceProvider sp) => Resolve<IReadRepository<TEntity, TKey>>(sp, BatchOp.GetAll);

    private static T Resolve<T>(IServiceProvider sp, BatchOp op) where T : class
        => sp.GetService<T>()
           ?? throw new BatchOperationNotSupportedException($"Entity '{typeof(TEntity).Name}' does not support op '{op}' (no {typeof(T).Name} registered).");

    private static TEntity Entity(JsonObject? body, JsonSerializerOptions json)
        => body is null
            ? throw new BatchRefException("Write step requires a body.")
            : body.Deserialize<TEntity>(json) ?? throw new BatchRefException("Body did not deserialize to an entity.");

    private static TKey Key(JsonElement id, JsonSerializerOptions json)
        => id.Deserialize<TKey>(json) ?? throw new BatchRefException("Could not read step 'id'.");
}
