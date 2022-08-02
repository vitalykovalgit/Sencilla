﻿
namespace Sencilla.Core.Injection.Impl
{
    /// <summary>
    /// Register all interfaces for type from attribute 
    /// NOTE: This class will be automatically registered in container
    /// </summary>
    public class ImplementAttributeRegistrator : ITypeRegistrator
    {
        public void Register(IContainer container, Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(ImplementAttribute), true);
            foreach (ImplementAttribute attribute in attributes)
            {
                container.RegisterType(attribute.Interface, type);
            }
        }
    }
}
