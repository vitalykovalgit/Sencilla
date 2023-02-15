namespace Sencilla.Component.Security;

public class SecurityConstraintRegistrator : ITypeRegistrator
{
    /// <summary>
    /// Register command hadlers automatically 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IContainer container, Type type)
    {
        if (type.IsAssignableTo(typeof(IBaseEntity)) && type.IsClass && !type.IsAbstract && !type.IsGenericType)
        {
            var @event = typeof(EntityReadingEvent<>).MakeGenericType(type);
            var @interface = typeof(IEventHandlerBase<>).MakeGenericType(@event);
            var constraint = typeof(SecurityConstraintHandler<>).MakeGenericType(type);
            container.RegisterType(@interface, constraint);
        }
    }
}
