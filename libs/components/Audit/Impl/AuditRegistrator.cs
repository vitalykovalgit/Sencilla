namespace Sencilla.Component.Audit;

/// <summary>Registers <see cref="AuditHandler{T}"/> for every entity that opts into auditing via <see cref="IEntityAuditable"/>.</summary>
public class AuditRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false })
            return;
        if (!type.IsAssignableTo(typeof(IEntityAuditable)))
            return;

        var handler = typeof(AuditHandler<>).MakeGenericType(type);
        container.AddTransient(typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatedEvent<>).MakeGenericType(type)), handler);
        container.AddTransient(typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityUpdatingEvent<>).MakeGenericType(type)), handler);
        container.AddTransient(typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityDeletingEvent<>).MakeGenericType(type)), handler);
    }
}
