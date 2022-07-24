
namespace Sencilla.Core.Component
{
    /// <summary>
    /// 
    /// </summary>
    public class ComponentBuilderOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public List<Type> Components { get; } = new List<Type>();

        /// <summary>
        /// Add component 
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public ComponentBuilderOptions Add<TComponent>() where TComponent : IComponent
        {
            Components.Add(typeof(TComponent));
            return this;
        }
    }
}
