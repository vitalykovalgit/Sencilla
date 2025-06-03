
namespace Sencilla.Core;

public abstract class EntityBaseEvent<TEntity> : Event
{
    /// <summary>
    /// 
    /// </summary>
    public IFilter? Filter { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IQueryable<TEntity>? Entities { get; set; }
}
