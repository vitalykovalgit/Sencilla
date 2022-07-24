
namespace Sencilla.Core
{
    /// <summary>
    /// Used to check if entity is creatable 
    /// without generic parameters
    /// </summary>
    public interface IBaseEntityCreateable : IBaseEntity
    {
        DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface IEntityCreateable<TKey> : IEntity<TKey>, IBaseEntityCreateable
    {
    }

    /// <summary>
    /// interafce with generic parameters as int by default 
    /// </summary>
    public interface IEntityCreateable : IEntityCreateable<int>
    {
    }
}
