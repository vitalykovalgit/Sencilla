using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore;

public static class EntityFrameworkCoreExtensions
{
    /// <summary>The <c>$action</c> a MERGE's OUTPUT gives a row it inserted.</summary>
    private const string InsertAction = "INSERT";

    /// <summary>
    /// Inserts every row no existing row matches on <paramref name="keys"/> (an insert-only MERGE) and returns the
    /// primary keys of the rows it inserted and of every row matching the keys. It reads no row itself: the caller
    /// reads them back by key, through whatever judges a read.
    /// </summary>
    public static async Task<(IReadOnlyList<object> Inserted, IReadOnlyList<object> Matched)> GetOrCreateBulkAsync<TEntity, TContext>(
        this TContext context,
        IEnumerable<TEntity> entities,
        string[] keys,
        CancellationToken token = default)
        where TEntity : class
        where TContext : DbContext
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return ([], []);

        var (mergeQuery, selectQuery) = new GetOrCreateQueryBuilder<TEntity>(keys).Build(entityList);

        // Result set 0 is the MERGE's OUTPUT (an insert-only MERGE: every row is one it inserted), result set 1 the SELECT.
        var inserted = new List<object>();
        var matched = new List<object>();
        await ReadRowsAsync(context, mergeQuery + "\n" + selectQuery, (set, row) =>
        {
            if (set == 0)
                inserted.Add(row.GetValue(1));
            else
                matched.Add(row.GetValue(0));
        }, token);

        return (inserted, matched);
    }

    public static Task UpsertAsync<TEntity, TContext>(this TContext context,
        TEntity e,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null)
        where TEntity : class
        where TContext : DbContext
    {
        return UpsertBulkAsync(context, new List<TEntity> { e }, condition, insertAction, updateAction);
    }

    /// <summary>Upserts <paramref name="entities"/> in one MERGE and returns the primary keys of the rows it inserted.</summary>
    public static Task<IReadOnlyList<object>> UpsertBulkAsync<TEntity, TContext>(this TContext context,
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
        where TEntity : class
        where TContext : DbContext
    {
        var cmnd = new UpsertCommand<TEntity>(condition)
        {
            InsertAction = insertAction,
            UpdateAction = updateAction,
        };

        var builder = new UpsertQueryBuilder<TEntity>(cmnd);

        var query = builder.Build(entities);

        return ReadInsertedKeysAsync(context, query, token);
    }

    public static Task MergeAsync<TEntity, TContext>(this TContext context,
        TEntity e,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null)
        where TEntity : class
        where TContext : DbContext
    {
        return MergeBulkAsync(context, new List<TEntity> { e }, condition, insertAction, updateAction);
    }

    /// <summary>
    /// Merges <paramref name="entities"/> in one MERGE — inserts, updates, and deletes every row not among them — and
    /// returns the primary keys of the rows it inserted.
    /// </summary>
    public static async Task<IReadOnlyList<object>> MergeBulkAsync<TEntity, TContext>(this TContext context,
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
        where TEntity : class
        where TContext : DbContext
    {
        // Nothing to merge — bail out. An empty source would otherwise emit a phantom
        // VALUES row (typed [Id] = 0, which clashes with a uniqueidentifier key) AND an
        // unscoped "WHEN NOT MATCHED BY SOURCE THEN DELETE" that would wipe the whole table.
        if (entities is null || !entities.Any())
            return [];

        var cmnd = new MergeCommand<TEntity>(condition)
        {
            InsertAction = insertAction,
            UpdateAction = updateAction
        };

        var builder = new MergeQueryBuilder<TEntity>(cmnd);

        var query = builder.Build(entities);

        return await ReadInsertedKeysAsync(context, query, token);
    }

    /// <summary>The primary keys a MERGE inserted, read from its OUTPUT (<see cref="QueryProvider.ToOutputMergeQuery"/>).</summary>
    private static async Task<IReadOnlyList<object>> ReadInsertedKeysAsync(DbContext context, string mergeQuery, CancellationToken token)
    {
        var inserted = new List<object>();
        await ReadRowsAsync(context, mergeQuery, (_, row) =>
        {
            if (row.GetString(0) == InsertAction)
                inserted.Add(row.GetValue(1));
        }, token);

        return inserted;
    }

    /// <summary>
    /// Runs a hand-built batch as EF runs its own commands — on the context's connection and transaction, with its
    /// timeout, logging and interceptors — and hands every row of every result set to <paramref name="read"/> with the
    /// set's index. Built without parameters, the text is never run through string.Format (ExecuteSqlRaw's is), so the
    /// braces of an inlined JSON value need no escaping.
    /// </summary>
    private static async Task ReadRowsAsync(DbContext context, string sql, Action<int, DbDataReader> read, CancellationToken token)
    {
        var command = context.GetService<IRawSqlCommandBuilder>().Build(sql);
        var execution = new RelationalCommandParameterObject(
            context.GetService<IRelationalConnection>(), null, null, context, context.GetService<IRelationalCommandDiagnosticsLogger>());

        await using var reader = await command.ExecuteReaderAsync(execution, token);
        var set = 0;
        do
        {
            while (await reader.ReadAsync(token))
                read(set, reader.DbDataReader);
            set++;
        }
        while (await reader.DbDataReader.NextResultAsync(token));
    }
}
