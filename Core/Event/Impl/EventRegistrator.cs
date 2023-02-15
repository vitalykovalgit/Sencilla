﻿namespace Sencilla.Core;

public class EventRegistrator : ITypeRegistrator
{
    /// <summary>
    /// Register event hadlers automatically 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IContainer container, Type type)
    {
        if (type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            var eventHandlerInterfaces = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandlerBase<>));
            foreach (var handlerInterface in eventHandlerInterfaces)
            {
                container.RegisterType(handlerInterface, type);
            }
        }
    }
}
