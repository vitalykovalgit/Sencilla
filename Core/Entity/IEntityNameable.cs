
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEntityNameable : IBaseEntity
    {
        /// <summary>
        /// Name of the entity 
        /// </summary>
        string Name { get; set; }
    }
}
