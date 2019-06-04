using Sencilla.Core.Builder;
using Sencilla.Core.Builder.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        /// <summary>
        /// Register sencilla framework builder, 
        /// ISencillaBuilder must be resolved in configure method to configure sencilla
        /// </summary>
        /// <param name="services"> Service collection </param>
        public static IServiceCollection AddSencillaFramework(this IServiceCollection builder)
        {
            builder.AddSingleton<ISencillaBuilder, AspNetCoreSencillaBuilder>();
            return builder;
        }
    }
}
