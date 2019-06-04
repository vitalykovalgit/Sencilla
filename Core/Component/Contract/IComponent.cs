using Sencilla.Core.Injection;

namespace Sencilla.Core.Component
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
        void Init(IResolver resolver);
        
    }
}
