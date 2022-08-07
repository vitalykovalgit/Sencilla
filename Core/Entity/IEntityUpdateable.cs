
namespace Sencilla.Core
{
    /// <summary>
    /// Allow entity to be updatable 
    /// </summary>
    public interface IEntityUpdateable : IBaseEntity
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEntityUpdateableTrack : IEntityUpdateable
    {
        /// <summary>
        /// Last updated date time 
        /// </summary>
        DateTime UpdatedDate { get; set; }
    }
}
