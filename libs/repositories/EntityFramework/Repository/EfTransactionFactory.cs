namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Entity Framework implementation of <see cref="ITransactionFactory"/>. Opens the transaction
/// on <typeparamref name="TContext"/> — the scoped instance every repository bound to that
/// context already shares — so their writes enlist without being told:
/// <see cref="BaseRepository{TContext}.InTransaction"/> sees CurrentTransaction and joins it
/// instead of opening its own.
/// </summary>
[DisableInjection]
internal sealed class EfTransactionFactory<TContext>(TContext context) : ITransactionFactory<TContext>
    where TContext : DbContext
{
    public async Task<IDbTransaction> Begin(CancellationToken token = default)
    {
        // Already inside one (a nested Begin, or a repository's BeginTransaction further out):
        // the outermost caller owns the commit. Non-relational providers such as InMemory have
        // no transactions at all, so callers get an inert handle rather than an exception.
        if (!context.Database.IsRelational() || context.Database.CurrentTransaction != null)
            return new NestedDbTransaction(context);

        return new EFDbTransaction(await context.Database.BeginTransactionAsync(token));
    }
}

/// <summary>
/// Handle for a <see cref="EfTransactionFactory{TContext}.Begin"/> that found a transaction
/// already open. Committing is a no-op — the outermost caller owns that — but rolling back
/// discards the whole ambient transaction: an inner "throw this away" must never end up
/// committed by the outer scope, and the outer commit then fails loudly instead of writing
/// half the unit.
/// </summary>
[DisableInjection]
internal sealed class NestedDbTransaction(DbContext context) : IDbTransaction
{
    public Task CommitAsync(CancellationToken token = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken token = default)
        => context.Database.CurrentTransaction?.RollbackAsync(token) ?? Task.CompletedTask;

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
