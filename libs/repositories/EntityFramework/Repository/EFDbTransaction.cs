namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Entity Framework Core implementation of IDbTransaction that wraps IDbContextTransaction.
/// DisableInjection prevents auto-discovery from registering this in DI — 
/// IDbContextTransaction is not a DI service; instances are created manually via DbContext.Database.BeginTransaction().
/// </summary>
[DisableInjection]
internal class EFDbTransaction(IDbContextTransaction transaction) : IDbTransaction
{
    private readonly IDbContextTransaction _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

    public Task CommitAsync(CancellationToken token = default) => _transaction.CommitAsync(token);
    public Task RollbackAsync(CancellationToken token = default) => _transaction.RollbackAsync(token);
    public void Dispose() => _transaction?.Dispose();
    public ValueTask DisposeAsync() => _transaction?.DisposeAsync() ?? ValueTask.CompletedTask;
}
