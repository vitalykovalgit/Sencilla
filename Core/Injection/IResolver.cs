
using System;

namespace Sencilla.Core.Injection
{
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
        TType Resolve<TType>();

        /// <summary>
        /// Resolve instance of specific type 
        /// </summary>
        /// <param name="type"> Type to be resolved </param>
        /// <returns> Instance of specific type </returns>
        object Resolve(Type type);
    }
}
