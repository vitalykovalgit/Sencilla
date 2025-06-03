namespace Sencilla.Messaging;

public class MessagingRegistrator : ITypeRegistrator
{
    /// <summary>
    /// Register event handlers automatically 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IContainer container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false }) return;
        
        var handlerInterfaces = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
        foreach (var handlerInterface in handlerInterfaces)
        {
            container.RegisterType(handlerInterface, type);
        }
    }
}
