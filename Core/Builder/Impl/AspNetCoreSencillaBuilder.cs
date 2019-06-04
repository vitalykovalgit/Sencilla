using Sencilla.Core.Injection;

namespace Sencilla.Core.Builder.Impl
{
    class AspNetCoreSencillaBuilder : ISencillaBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public AspNetCoreSencillaBuilder(IResolver resolver)
        {
            Resolver = resolver;
        }

        /// <summary>
        /// 
        /// </summary>
        public IResolver Resolver { get; }
    }
}
