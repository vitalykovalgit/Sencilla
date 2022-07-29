
namespace Sencilla.Core
{
    /// <summary>
    /// Item can be marked in DB as deleted 
    /// </summary>
    public interface IBaseEntityRemoveable : IBaseEntity
    {
        DateTime? DeletedDate { get; set; }
    }

    /// <summary>
    /// Item can be marked in DB as deleted 
    /// </summary>
    public interface IEntityRemoveable<TKey> : IEntityUpdateable<TKey>, IBaseEntityRemoveable
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEntityRemoveable : IEntityRemoveable<int>
    { 
    }

}
