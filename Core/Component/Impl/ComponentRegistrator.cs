namespace Sencilla.Core.Impl
{
    public class ComponentRegistrator : ITypeRegistrator
    {
        /// <summary>
        /// Contains list of all components in applications 
        /// </summary>
        public List<IComponent> Components { get; } = new List<IComponent>();

        /// <summary>
        /// Check if type is component register in in container 
        /// and add instance to <see cref="ComponentRegistrator.Components"/> property 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="type"></param>
        public void Register(IRegistrator container, Type type)
        {
            var componentInterface = typeof(IComponent);
            var isComponent = componentInterface.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract;
            if (isComponent)
            {
                container.RegisterType(componentInterface, type, type.FullName);
                var component = Activator.CreateInstance(type) as IComponent;
                if (component != null)
                {
                    Components.Add(component);   
                }
            }
        }

        /// <summary>
        /// Initialize all founded components 
        /// </summary>
        /// <param name="container"></param>
        public void InitComponents(IRegistrator container)
        {
            foreach (var c in Components)
            {
                c.Init(container);
            }
        }
    }
}
