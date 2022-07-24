
namespace Sencilla.Core
{
    /// <summary>
    /// Allow entity to be updatable 
    /// </summary>
    public interface IBaseEntityUpdateable : IBaseEntity
    {
        /// <summary>
        /// Last updated date time 
        /// </summary>
        DateTime UpdatedDate { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
	public interface IEntityUpdateable<TKey> : IEntity<TKey>, IBaseEntityUpdateable
    {
    }

    public interface IEntityUpdateable : IEntityUpdateable<int>
    {
    }
}
