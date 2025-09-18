namespace Sencilla.Core;

public class EventRegistrator : ITypeRegistrator
{
    /// <summary>
    /// Register event hadlers automatically 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IServiceCollection container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false }) return;
        
        var eventHandlerInterfaces = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandlerBase<>));
        foreach (var handlerInterface in eventHandlerInterfaces)
        {
            container.AddTransient(handlerInterface, type);
        }
    }
}
