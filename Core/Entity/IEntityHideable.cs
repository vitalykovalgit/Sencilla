
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEntityHideable : IBaseEntity
    {
        /// <summary>
        /// True if entity is hidden 
        /// </summary>
        bool Hidden { get; set; }
    }
}
