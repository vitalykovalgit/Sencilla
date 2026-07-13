namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class CreateRepository<TEntity, TContext> : CreateRepository<TEntity, TContext, int>, ICreateRepository<TEntity>
   where TEntity : class, IEntity<int>, IEntityCreateable, new()
   where TContext : DbContext
{
    public CreateRepository(RepositoryDependency dependency, TContext context): base(dependency, context) { }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class CreateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, ICreateRepository<TEntity, TKey>
       where TEntity : class, IEntity<TKey>, IEntityCreateable, new()
       where TContext : DbContext
{
    public CreateRepository(RepositoryDependency dependency, TContext context): base(dependency, context)
    {
    }

    public async Task<TEntity?> Create(TEntity entity, CancellationToken token = default)
    {
        return (await Create(new[] { entity })).FirstOrDefault();
    }

    public Task<IEnumerable<TEntity>> Create(params TEntity[] entities)
    {
        return Create(entities, CancellationToken.None);
    }

    public virtual Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        // Materialise once: a lazy IEnumerable (e.g. a Select projection) would yield
        // FRESH instances on every re-enumeration, so the instances EF saves and the
        // instances the post-image reads keys from would differ — keys would read back
        // as default and the security post-image would match zero rows (vacuous pass).
        var items = entities as IReadOnlyList<TEntity> ?? entities.ToList();

        return InTransaction<IEnumerable<TEntity>>(async () =>
        {
            // Check constraints
            var eventCreating = new EntityCreatingEvent<TEntity> { Entities = items.AsQueryable() };
            await D.Events.PublishAsync(eventCreating, token);

            // update creation date
            // TODO: Move to event handler
            foreach (var e in items)
            {
                if (e is IEntityCreateableTrack)
                   ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;
            }

            // Add to context and save
            DbContext.AddRange(items);
            await Save(token);

            // Notify about — with a post-image DB query of the inserted rows (keys are
            // populated by the save), so constraint handlers can enforce CREATE against
            // real DB state instead of the client-supplied objects.
            var ids = items.Select(e => e.Id).ToList();
            await PublishCreatedWithPostImage(items, x => ids.Contains(x.Id), token);

            return items;
        }, token);
    }

    public async Task<TEntity> UpsertAsync(TEntity entity,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        await UpsertAsync([entity], condition, insertAction, updateAction, token);

        return entity;
    }

    public Task<IEnumerable<TEntity>> UpsertAsync(Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        params TEntity[] entities) => UpsertAsync(entities, condition, insertAction, updateAction);

    public Task<IEnumerable<TEntity>> UpsertAsync(IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        return InTransaction<IEnumerable<TEntity>>(async () =>
        {
            if (!entities.Any())
                return entities;

            // Existence probe for the per-row permission split (see helpers below).
            var keyProps = ExtractKeyProperties(condition);
            var keyPredicate = BuildKeyPredicate(keyProps, entities)!;
            var existingRows = await DbContext.Set<TEntity>().AsNoTracking().Where(keyPredicate).ToListAsync(token);
            var existingKeys = existingRows.Select(e => KeyOf(keyProps, e)).ToHashSet();
            var toCreate = entities.Where(e => !existingKeys.Contains(KeyOf(keyProps, e))).ToList();

            // The update-side query targets only the pre-existing keys, so the
            // post-image check cannot accidentally cover freshly inserted rows.
            var updateQuery = existingRows.Count > 0
                ? DbContext.Set<TEntity>().AsNoTracking().Where(BuildKeyPredicate(keyProps, existingRows)!)
                : null;

            if (toCreate.Count > 0)
                await D.Events.PublishAsync(new EntityCreatingEvent<TEntity> { Entities = toCreate.AsQueryable() }, token);

            if (updateQuery != null)
            {
                var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = entities.AsQueryable(), DbEntities = updateQuery };
                await D.Events.PublishAsync(eventUpdating, token);
                await ThrowIfNarrowed(eventUpdating.DbEntities, updateQuery, token);
            }

            // update creation date
            // TODO: Move to event handler
            foreach (var e in entities)
            {
                if (e is IEntityCreateableTrack)
                    ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;
            }

            await DbContext.UpsertBulkAsync(entities, condition, insertAction, updateAction);

            if (updateQuery != null)
            {
                var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = entities.AsQueryable(), DbEntities = updateQuery };
                await D.Events.PublishAsync(eventUpdated, token);
                await ThrowIfNarrowed(eventUpdated.DbEntities, updateQuery, token);
            }

            if (toCreate.Count > 0)
                await PublishCreatedWithPostImage(toCreate, BuildKeyPredicate(keyProps, toCreate)!, token);

            // TODO: Think about returning values
            return entities;
        }, token);
    }

    public async Task<TEntity> MergeAsync(TEntity entity,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        await MergeAsync([entity], condition, insertAction, updateAction, token);

        return entity;
    }

    public Task<IEnumerable<TEntity>> MergeAsync(Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        params TEntity[] entities) => MergeAsync(entities, condition, insertAction, updateAction);

    public Task<IEnumerable<TEntity>> MergeAsync(IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        return InTransaction<IEnumerable<TEntity>>(async () =>
        {
            // MergeBulkAsync bails on an empty source (it would otherwise wipe the
            // table) — mirror that before running any checks.
            if (!entities.Any())
                return entities;

            // Existence probe for the per-row permission split (see helpers below).
            var keyProps = ExtractKeyProperties(condition);
            var keyPredicate = BuildKeyPredicate(keyProps, entities)!;
            var existingRows = await DbContext.Set<TEntity>().AsNoTracking().Where(keyPredicate).ToListAsync(token);
            var existingKeys = existingRows.Select(e => KeyOf(keyProps, e)).ToHashSet();
            var toCreate = entities.Where(e => !existingKeys.Contains(KeyOf(keyProps, e))).ToList();

            var updateQuery = existingRows.Count > 0
                ? DbContext.Set<TEntity>().AsNoTracking().Where(BuildKeyPredicate(keyProps, existingRows)!)
                : null;

            // Merge additionally deletes every row NOT matched by the incoming set —
            // those removals are governed by the Delete permission.
            var deleteQuery = DbContext.Set<TEntity>().AsNoTracking().Where(Not(keyPredicate));
            var hasRemovals = await deleteQuery.AnyAsync(token);

            if (toCreate.Count > 0)
                await D.Events.PublishAsync(new EntityCreatingEvent<TEntity> { Entities = toCreate.AsQueryable() }, token);

            if (updateQuery != null)
            {
                var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = entities.AsQueryable(), DbEntities = updateQuery };
                await D.Events.PublishAsync(eventUpdating, token);
                await ThrowIfNarrowed(eventUpdating.DbEntities, updateQuery, token);
            }

            if (hasRemovals)
            {
                var eventDeleting = new EntityDeletingEvent<TEntity> { Entities = deleteQuery };
                await D.Events.PublishAsync(eventDeleting, token);
                await ThrowIfNarrowed(eventDeleting.Entities, deleteQuery, token);
            }

            // update creation date
            // TODO: Move to event handler
            foreach (var e in entities)
                if (e is IEntityCreateableTrack)
                    ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;

            await DbContext.MergeBulkAsync(entities, condition, insertAction, updateAction);

            if (updateQuery != null)
            {
                var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = entities.AsQueryable(), DbEntities = updateQuery };
                await D.Events.PublishAsync(eventUpdated, token);
                await ThrowIfNarrowed(eventUpdated.DbEntities, updateQuery, token);
            }

            if (hasRemovals)
                await D.Events.PublishAsync(new EntityDeletedEvent<TEntity>(), token);

            if (toCreate.Count > 0)
                await PublishCreatedWithPostImage(toCreate, BuildKeyPredicate(keyProps, toCreate)!, token);

            // TODO: Think about returning values
            return entities;
        }, token);
    }

    public Task<GetOrCreateResult<TEntity>> GetOrCreateAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        => GetOrCreateAsync(entities, Array.Empty<string>(), token);

    public Task<GetOrCreateResult<TEntity>> GetOrCreateAsync(IEnumerable<TEntity> entities, params Expression<Func<TEntity, object>>[] keys)
        => GetOrCreateAsync(entities, keys.Select(ExtractMemberName).ToArray(), CancellationToken.None);

    public Task<GetOrCreateResult<TEntity>> GetOrCreateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>>[] keys, CancellationToken token)
        => GetOrCreateAsync(entities, keys.Select(ExtractMemberName).ToArray(), token);

    public Task<GetOrCreateResult<TEntity>> GetOrCreateAsync(IEnumerable<TEntity> entities, params string[] keys)
        => GetOrCreateAsync(entities, keys, CancellationToken.None);

    public Task<GetOrCreateResult<TEntity>> GetOrCreateAsync(IEnumerable<TEntity> entities, string[] keys, CancellationToken token)
        => GetOrCreateAsync(entities, keys, filter: null, token);

    public Task<GetOrCreateResult<TEntity>> GetOrCreateAsync(IEnumerable<TEntity> entities, string[] keys, Filter? filter, CancellationToken token = default)
    {
        return InTransaction(async () =>
        {
            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return new GetOrCreateResult<TEntity> { Created = [], Existing = [] };

            // Per-row permission mapping: only rows that will actually be inserted
            // are checked as CREATE — matching rows are just read back. With an extra
            // match filter the probe can't predict the split, so all rows are checked
            // (over-strict, never under-enforced).
            var toCreate = entityList;
            if (filter == null)
            {
                var keyProps = ResolveKeyProperties(keys);
                var keyPredicate = BuildKeyPredicate(keyProps, entityList)!;
                var existingKeys = (await DbContext.Set<TEntity>().AsNoTracking().Where(keyPredicate).ToListAsync(token))
                    .Select(e => KeyOf(keyProps, e)).ToHashSet();
                toCreate = entityList.Where(e => !existingKeys.Contains(KeyOf(keyProps, e))).ToList();
            }

            if (toCreate.Count > 0)
                await D.Events.PublishAsync(new EntityCreatingEvent<TEntity> { Entities = toCreate.AsQueryable() }, token);

            foreach (var e in entityList)
                if (e is IEntityCreateableTrack trackable)
                    trackable.CreatedDate = DateTime.UtcNow;

            var (created, existing) = await DbContext.GetOrCreateBulkAsync(entityList, keys, filter, token);

            var createdList = created.ToList();
            if (createdList.Count > 0)
                await PublishCreatedWithPostImage(createdList, BuildKeyPredicate(ResolveKeyProperties(keys), createdList)!, token);

            return new GetOrCreateResult<TEntity> { Created = createdList, Existing = existing };
        }, token);
    }

    /// <summary>
    /// Publishes <see cref="EntityCreatedEvent{TEntity}"/> carrying a post-image DB
    /// query of the inserted rows, then denies the batch (rollback via the ambient
    /// transaction) when a constraint handler narrowed the query. The narrowing is
    /// composed lazily and only evaluated here — after every handler ran — so rows
    /// provisioned by created-event handlers (e.g. the creator's role row) are
    /// visible to the check regardless of handler ordering.
    /// </summary>
    private async Task PublishCreatedWithPostImage(IEnumerable<TEntity> created, Expression<Func<TEntity, bool>> insertedRows, CancellationToken token)
    {
        var postImage = DbContext.Set<TEntity>().AsNoTracking().Where(insertedRows);
        var eventCreated = new EntityCreatedEvent<TEntity> { Entities = created.AsQueryable(), DbEntities = postImage };
        await D.Events.PublishAsync(eventCreated, token);
        await ThrowIfNarrowed(eventCreated.DbEntities, postImage, token);
    }

    private static string ExtractMemberName(Expression<Func<TEntity, object>> expr)
    {
        var body = expr.Body is UnaryExpression unary ? unary.Operand : expr.Body;
        if (body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Key selector must be a member access expression.", nameof(expr));
    }

    // ── Per-row permission mapping for upsert-style operations ────────────────
    // Rows whose match key already exists in the DB are an UPDATE (pre+post image
    // checks against DB state); the rest are a CREATE (in-memory validation).
    // Checked separately so a create-only role can insert without update rights
    // and vice versa — and so an upsert can never mutate rows the caller could
    // not have updated directly.

    /// <summary>
    /// Key members of an upsert/merge match condition: x => x.Name or x => new { x.A, x.B }.
    /// </summary>
    private static List<PropertyInfo> ExtractKeyProperties(Expression<Func<TEntity, object>> condition)
    {
        var body = condition.Body is UnaryExpression unary ? unary.Operand : condition.Body;
        var props = body switch
        {
            NewExpression n => n.Arguments.OfType<MemberExpression>().Select(m => m.Member).OfType<PropertyInfo>().ToList(),
            MemberExpression m when m.Member is PropertyInfo p => [p],
            _ => new List<PropertyInfo>(),
        };

        if (props.Count == 0)
            throw new ArgumentException("Match condition must select a property or an anonymous object of properties.", nameof(condition));

        return props;
    }

    private static List<PropertyInfo> ResolveKeyProperties(string[] keys)
    {
        // Same default as GetOrCreateQueryBuilder: no keys → match by Id.
        var effectiveKeys = keys.Length > 0 ? keys : ["Id"];
        return effectiveKeys
            .Select(k => typeof(TEntity).GetProperty(k, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new ArgumentException($"Key '{k}' is not a property of {typeof(TEntity).Name}."))
            .ToList();
    }

    /// <summary>
    /// DB predicate matching rows whose key columns equal any incoming entity's
    /// key values — the existence probe for the per-row permission split.
    /// </summary>
    private static Expression<Func<TEntity, bool>>? BuildKeyPredicate(List<PropertyInfo> keyProps, IEnumerable<TEntity> entities)
    {
        Expression<Func<TEntity, bool>>? predicate = null;
        foreach (var entity in entities)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            Expression? match = null;
            foreach (var prop in keyProps)
            {
                var eq = Expression.Equal(Expression.Property(parameter, prop), Expression.Constant(prop.GetValue(entity), prop.PropertyType));
                match = match == null ? eq : Expression.AndAlso(match, eq);
            }

            var entityMatch = Expression.Lambda<Func<TEntity, bool>>(match!, parameter);
            // OrElse is a no-op on a null receiver — seed explicitly.
            predicate = predicate == null ? entityMatch : predicate.OrElse(entityMatch);
        }

        return predicate;
    }

    private static Expression<Func<TEntity, bool>> Not(Expression<Func<TEntity, bool>> expr) =>
        Expression.Lambda<Func<TEntity, bool>>(Expression.Not(expr.Body), expr.Parameters);

    /// <summary>
    /// Composite key identity of an entity for the in-memory side of the existence
    /// split. String keys may compare case-sensitively here while the DB collation
    /// does not — a mismatch only moves a row into BOTH the create and update
    /// checks, never out of enforcement (fail-closed).
    /// </summary>
    private static string KeyOf(List<PropertyInfo> keyProps, TEntity entity) =>
        string.Join("\u001f", keyProps.Select(p => p.GetValue(entity)?.ToString() ?? "\u0000"));
}
