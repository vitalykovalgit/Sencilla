using System.Linq;
using System.Collections.Generic;

using Sencilla.Core.Component;

namespace Sencilla.Impl.Component
{
    /// <summary>
    /// Unity implementation of IComponent manager interface
    /// </summary>
    public class ComponentManager : IComponentManager
    {
        private readonly Dictionary<string, IComponent> _components;

        /// <summary>
        /// Initialize components 
        /// </summary>
        /// <param name="components"></param>
        public ComponentManager(IComponent[] components)
        {
            Components = components;
            _components = components.ToDictionary(c=>c.Type.ToLower());
        }

        /// <summary>
        /// List of components 
        /// </summary>
        public IEnumerable<IComponent> Components { get; }

        /// <summary>
        /// Get component by a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IComponent GetComponent(string type)
        {
            var key = type.ToLower();
            return _components.ContainsKey(key) ? _components[key] : null;
        }

        public IComponent Load(string path)
        {
            throw new System.NotImplementedException();
        }

        public IComponent Unload()
        {
            throw new System.NotImplementedException();
        }
    }
}
