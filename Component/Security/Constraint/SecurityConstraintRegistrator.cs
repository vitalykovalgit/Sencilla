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
            var constraint = typeof(SecurityConstraintHandler<>).MakeGenericType(type);

            // read interafce 
            var readInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityReadingEvent<>).MakeGenericType(type));
            container.RegisterType(readInterface, constraint);

            // create interafce 
            var createInterface = typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatingEvent<>).MakeGenericType(type));
            container.RegisterType(createInterface, constraint);
        }
    }
}
