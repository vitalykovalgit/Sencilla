
namespace Sencilla.Core
{
    /// <summary>
    /// Base interface for all components 
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Component type
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Initialize the component 
        /// </summary>
        void Init(IContainer container);
        
    }
}
