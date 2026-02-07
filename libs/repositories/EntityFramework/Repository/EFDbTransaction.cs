namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Entity Framework Core implementation of IDbTransaction that wraps IDbContextTransaction
/// </summary>
internal class EFDbTransaction(IDbContextTransaction transaction) : IDbTransaction
{
    private readonly IDbContextTransaction _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

    public Task CommitAsync(CancellationToken token = default) => _transaction.CommitAsync(token);
    public Task RollbackAsync(CancellationToken token = default) => _transaction.RollbackAsync(token);
    public void Dispose() => _transaction?.Dispose();
    public ValueTask DisposeAsync() => _transaction?.DisposeAsync() ?? ValueTask.CompletedTask;
}
