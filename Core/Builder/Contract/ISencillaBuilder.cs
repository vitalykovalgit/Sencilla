
using Sencilla.Core.Injection;

namespace Sencilla.Core.Builder
{
    /// <summary>
    /// Sencilla framework builder. 
    /// Register all the components 
    /// </summary>
    public interface ISencillaBuilder
    {
        /// <summary>
        /// Register dependency injection 
        /// </summary>
        /// <typeparam name="TResolverImpl"></typeparam>
        //void AddResolver<TResolverImpl>() where TResolverImpl : class, IResolver;

        /// <summary>
        /// Retrive resolver implementation
        /// </summary>
        IResolver Resolver { get; }
    }
}
