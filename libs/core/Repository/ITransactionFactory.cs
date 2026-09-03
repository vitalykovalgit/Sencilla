namespace Sencilla.Core;

/// <summary>
/// Starts a transaction that spans every repository sharing one database context, so a
/// multi-entity write is an explicit unit of work instead of a transaction borrowed from
/// whichever repository happened to be at hand:
/// <code>
/// using var tx = await transactions.Begin(token);
/// await orders.Create(order, token);
/// await items.Create(lines, token);
/// await tx.CommitAsync(token);
/// </code>
/// Re-entrant: when a transaction is already open — an outer <see cref="Begin"/>, or a
/// repository's <c>BeginTransaction</c> further up the call stack — the returned handle
/// defers the commit to whoever opened it. Disposing without committing rolls back.
/// </summary>
public interface ITransactionFactory
{
    /// <summary>
    /// Opens a transaction on the underlying context, or joins the one already open.
    /// </summary>
    Task<IDbTransaction> Begin(CancellationToken token = default);
}

/// <summary>
/// The same, bound to a specific context — for entities mapped elsewhere than the default
/// one with <c>[DbContext&lt;T&gt;]</c>. A transaction never spans two contexts; group only
/// repositories that share <typeparamref name="TContext"/>.
/// </summary>
public interface ITransactionFactory<TContext> : ITransactionFactory;
