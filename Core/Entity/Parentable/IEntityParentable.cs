namespace Sencilla.Core
{
    /// <summary>
    /// Allow the tree organization of the entity 
    /// </summary>
    public interface IEntityParentable<TKey> : IEntity<TKey>
    {
        TKey ParentId { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEntityParentable : IEntityParentable<int>
    {
    }
}
