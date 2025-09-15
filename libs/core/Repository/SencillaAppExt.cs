namespace Sencilla.Core;

/// <summary>
/// Base repository for all the repos
/// </summary>
public static class SencillaAppRepositoryExt
{
    /// <summary>
    /// Save changes
    /// </summary>
    public static IReadRepository<TEntity> Reader<TEntity>(this ISencillaApp app) where TEntity : IEntity<int> => app.Provide<IReadRepository<TEntity>>();
    public static IReadRepository<TEntity, TKey> Reader<TEntity, TKey>(this ISencillaApp app) where TEntity : IEntity<TKey> => app.Provide<IReadRepository<TEntity, TKey>>();


    public static IUpdateRepository<TEntity> Updator<TEntity>(this ISencillaApp app) where TEntity : IEntity<int> => app.Provide<IUpdateRepository<TEntity>>();
    public static IUpdateRepository<TEntity, TKey> Updator<TEntity, TKey>(this ISencillaApp app) where TEntity : IEntity<TKey> => app.Provide<IUpdateRepository<TEntity, TKey>>();


    public static ICreateRepository<TEntity> Creator<TEntity>(this ISencillaApp app) where TEntity : IEntity<int> => app.Provide<ICreateRepository<TEntity>>();
    public static ICreateRepository<TEntity, TKey> Creator<TEntity, TKey>(this ISencillaApp app) where TEntity : IEntity<TKey> => app.Provide<ICreateRepository<TEntity, TKey>>();


    public static IDeleteRepository<TEntity> Deletor<TEntity>(this ISencillaApp app) where TEntity : IEntity<int> => app.Provide<IDeleteRepository<TEntity>>();
    public static IDeleteRepository<TEntity, TKey> Deletor<TEntity, TKey>(this ISencillaApp app) where TEntity : IEntity<TKey> => app.Provide<IDeleteRepository<TEntity, TKey>>();

}

