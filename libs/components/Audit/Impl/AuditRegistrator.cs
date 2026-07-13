namespace Sencilla.Component.Audit;

/// <summary>
/// Registers <see cref="AuditHandler{T}"/> for every entity that opts into auditing via
/// <see cref="IEntityAuditable"/>. Runs from BOTH the <c>AddSencilla()</c> [AutoDiscovery] scan
/// and the <c>AddSencillaAudit()</c> fallback loop, so registration is idempotent — a duplicate
/// descriptor would make every audit handler fire twice (duplicate audit rows).
/// </summary>
public class AuditRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false })
            return;
        if (!type.IsAssignableTo(typeof(IEntityAuditable)))
            return;

        var handler = typeof(AuditHandler<>).MakeGenericType(type);
        AddOnce(container, typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatedEvent<>).MakeGenericType(type)), handler);
        AddOnce(container, typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityUpdatingEvent<>).MakeGenericType(type)), handler);
        AddOnce(container, typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityDeletingEvent<>).MakeGenericType(type)), handler);
    }

    static void AddOnce(IServiceCollection container, Type service, Type impl)
    {
        if (!container.Any(d => d.ServiceType == service && d.ImplementationType == impl))
            container.AddTransient(service, impl);
    }
}
