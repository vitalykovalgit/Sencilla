
namespace Sencilla.Core
{
    /// <summary>
    /// Register type in container 
    /// </summary>
    public interface IRegistrator
    {
        /// <summary>
        /// Register implemention for interface 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="implementation"></param>
        void RegisterType<TInterface, TImplementation>(string? name = null)
            where TInterface : class
            where TImplementation : class, TInterface;

        /// <summary>
        /// Register implemention for interface 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="implementation"></param>
        void RegisterType(Type @interface, Type implementation);

        /// <summary>
        /// Register implemention for interface with specific name 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="implementation"></param>
        /// <param name="name"></param>
        void RegisterType(Type @interface, Type implementation, string? name);

        /// <summary>
        /// Register instance for specific interface 
        /// </summary>
        /// <param name="iterface"></param>
        /// <param name="instance"></param>
        void RegisterInstance(Type @interface, object instance);

        /// <summary>
        /// Register instance for specific interface 
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instance"></param>
        void RegisterInstance<TInterface>(TInterface instance) where TInterface : class;
    }
}
