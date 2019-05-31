
using Sencilla.Core.Injection;

namespace Sencilla.Impl.Component
{
    /// <summary>
    /// Base class for all unity component
    /// </summary>
    public class UnityComponent
    {
        public IResolver Resolver { get; set; }

        public T R<T>()
        {
            return Resolver.Resolve<T>();
        }
    }
}
