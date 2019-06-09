using Sencilla.Core;
using Sencilla.Core.Builder;
using Sencilla.Core.Builder.Impl;
using Sencilla.Core.Logging;
using Sencilla.Core.Logging.Impl;
using Sencilla.Core.Security.Impl;

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
            // Add builder 
            builder.AddSingleton<ISencillaBuilder, AspNetCoreSencillaBuilder>();
            builder.AddTransient<ILogger, NoLogger>();

            // Security
            builder.AddTransient<IReadPermission, AllowAllPermissions>();
            builder.AddTransient<ICreatePermission, AllowAllPermissions>();
            builder.AddTransient<IUpdatePermission, AllowAllPermissions>();
            builder.AddTransient<IRemovePermission, AllowAllPermissions>();
            builder.AddTransient<IDeletePermission, AllowAllPermissions>();

            return builder;
        }
    }
}
