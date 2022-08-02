
namespace Sencilla.Core
{
    /// <summary>
    /// Allow to avoid registartion in container (e.g service collection)
    /// and automatically register interface on a class where this attribute 
    /// is applied
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ImplementAttribute : Attribute
    {
        /// <summary>
        /// Interface to be implemented 
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// Initialize Implement Attribute 
        /// </summary>
        /// <param name="interface"></param>
        public ImplementAttribute(Type @interface)
        {
            Interface = @interface;
        }
    }
}
