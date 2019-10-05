using System;
using System.Collections.Generic;

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
        /// Resolve type with specofoc instance
        /// </summary>
        /// <typeparam name="TType">Type to be resolved </typeparam>
        /// <param name="name"> Name of the instance </param>
        /// <returns>Instance of the type</returns>
        TType Resolve<TType>(string name);

        /// <summary>
        /// Resolve instance of specific type 
        /// </summary>
        /// <param name="type"> Type to be resolved </param>
        /// <returns> Instance of specific type </returns>
        object Resolve(Type type);

        /// <summary>
        /// Resolve all instance of provided TType 
        /// </summary>
        /// <typeparam name="TType">Type to resolve</typeparam>
        /// <returns> Collection of instances </returns>
        IEnumerable<TType> ResolveAll<TType>();

        /// <summary>
        /// Register implemention for interface 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="implementation"></param>
        void RegisterType<TInterface, TImplementation>(string name = null);

        /// <summary>
        /// Register implemention for interface 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="implementation"></param>
        void RegisterType(Type iterface, Type implementation);

        /// <summary>
        /// Register implemention for interface with specific name 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="implementation"></param>
        /// <param name="name"></param>
        void RegisterType(Type iterface, Type implementation, string name);

        /// <summary>
        /// Register instance for specific interface 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="instance"></param>
        void RegisterInstance(Type iterface, object instance);

        /// <summary>
        /// Register instance for specific interface 
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instance"></param>
        void RegisterInstance<TInterface>(TInterface instance);
    }
}
