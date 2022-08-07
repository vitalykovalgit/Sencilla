
namespace Sencilla.Core
{
    /// <summary>
    /// This interface is used to allow sorting of entity 
    /// </summary>
    public interface IEntityOrderable : IBaseEntity
    {   
        /// <summary>
        /// Order of the entity 
        /// </summary>
        int Order { get; set; }
    }
}
