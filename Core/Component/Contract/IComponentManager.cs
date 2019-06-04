using System.Collections.Generic;

namespace Sencilla.Core.Component
{
    /// <summary>
    /// Manage all components 
    /// </summary>
    public interface IComponentManager
    {
        
        /// <summary>
        /// Contains list of components loaded to PF
        /// </summary>
        IEnumerable<IComponent> Components { get; }

        /// <summary>
        /// Search component by type 
        /// </summary>
        /// <param name="type"> Type of the component </param>
        /// <returns> component instance or null if not found </returns>
        IComponent GetComponent(string type);

        /// <summary>
        /// Load component to pursuit framework dynamically 
        /// </summary>
        /// <param name="path">Path to the component</param>
        /// <returns> Instance to loaded component or threw exception if component can't be loaded </returns>
        /// <exception cref="InvalidArgumentException"></exception>
        IComponent Load(string path);

        /// <summary>
        /// Unload component from the system 
        /// </summary>
        /// <returns></returns>
        IComponent Unload();
    }
}
