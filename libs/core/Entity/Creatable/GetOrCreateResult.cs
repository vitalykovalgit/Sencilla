namespace Sencilla.Core;

public class GetOrCreateResult<TEntity>
{
    public IEnumerable<TEntity> Created { get; init; } = [];
    public IEnumerable<TEntity> Existing { get; init; } = [];
    public IEnumerable<TEntity> All => Created.Concat(Existing);
}
