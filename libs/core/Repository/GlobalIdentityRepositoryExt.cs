namespace Sencilla.Core;

public static class GlobalIdentityRepositoryExt
{
    public static async Task<TEntity?> GetByGlobalId<TEntity, TKey>(this IReadRepository<TEntity, TKey> repo, Guid globalId, CancellationToken token = default)
        where TEntity : IEntity<TKey>, IEntityGlobal
    {
        var filter = new Filter();
        filter.AddProperty(nameof(IEntityGlobal.GlobalId), typeof(Guid), globalId);
        return await repo.FirstOrDefault(filter, token);
    }
}
