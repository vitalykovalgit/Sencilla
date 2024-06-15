
namespace Sencilla.Core;

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
    IComponent? GetComponent(string type);
}
