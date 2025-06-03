
namespace Sencilla.Core;


/// <summary>
/// Resolve the instance of the type 
/// </summary>
public interface IResolver
{
    /// <summary>
    /// Resolve instance of specific type 
    /// </summary>
    /// <typeparam name="TType"> Type to be resolved </typeparam>
    /// <returns> Instance of TType </returns>
    TServcie? Resolve<TServcie>();

    /// <summary>
    /// Resolve type with specofoc instance
    /// </summary>
    /// <typeparam name="TType">Type to be resolved </typeparam>
    /// <param name="name"> Name of the instance </param>
    /// <returns>Instance of the type</returns>
    TServcie? Resolve<TServcie>(string name);

    /// <summary>
    /// Resolve instance of specific type 
    /// </summary>
    /// <param name="type"> Type to be resolved </param>
    /// <returns> Instance of specific type </returns>
    object? Resolve(Type type);

    /// <summary>
    /// Resolve all instance of provided TType 
    /// </summary>
    /// <typeparam name="TType">Type to resolve</typeparam>
    /// <returns> Collection of instances </returns>
    IEnumerable<TServcie> ResolveAll<TServcie>();

}
