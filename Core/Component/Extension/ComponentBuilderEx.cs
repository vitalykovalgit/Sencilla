using System;
using System.Linq;
using System.Reflection;

using Sencilla.Core.Component;

namespace Sencilla.Core.Builder
{
    public static class ComponentBuilderEx
    {
        /// <summary>
        /// Resolve all componets from container and call <see cref="IComponent.Init()"/> method
        /// </summary>
        /// <param name="container"></param>
        public static ISencillaBuilder BuildSencillaComponents(this ISencillaBuilder builder)
        {
            var resolver = builder.Resolver;
            var components = resolver.ResolveAll<IComponent>();
            foreach (var component in components)
            {
                component.Init(resolver);
            }

            return builder;
        }

        public static ISencillaBuilder UseSencillaComponents(this ISencillaBuilder builder, Action<ComponentBuilderOptions> componentsRetriver)
        {
            // Component retriever 
            var options = new ComponentBuilderOptions();
            componentsRetriver(options);

            // Register in container 
            var componentInterface = typeof(IComponent);
            foreach (var componentImpl in options.Components)
            {
                builder.Resolver.RegisterType(componentInterface, componentImpl, componentImpl.FullName);
            }

            // Init components
            return builder.BuildSencillaComponents();
        }

        /// <summary>
        /// Automatically finds all classes that implements <see cref="IComponent"/> interface
        /// and register it in dependency injection container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="assambly"></param>
        public static ISencillaBuilder AddSencillaComponents(this ISencillaBuilder builder, Assembly assambly)
        {
            var componentInterface = typeof(IComponent);
            var componnets = assambly.GetTypes().Where(t => t.IsClass && componentInterface.IsAssignableFrom(t));

            foreach (var componentImpl in componnets)
            {
                builder.Resolver.RegisterType(componentInterface, componentImpl, componentImpl.FullName);
            }

            return builder;
        }
    }
}
