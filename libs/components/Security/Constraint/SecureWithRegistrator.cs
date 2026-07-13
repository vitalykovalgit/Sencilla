namespace Sencilla.Component.Security;

/// <summary>
/// Wires every [SecureWith] declaration at startup (auto-discovered ITypeRegistrator):
/// - resolves and validates the chain — a malformed declaration (hop without Via,
///   non-terminating chain, cycle, key mismatch) fails the deploy, not the first request;
/// - for terminal declarations, registers <see cref="CreatorRoleProvisioningHandler{TEntity,TEntityKey,TLink,TLinkKey}"/>
///   (creator's role row, in-transaction) and <see cref="RoleableValidationHandler{TLink}"/>
///   (claims-derived roles are global-only) on the role table's write events.
///
/// Hop declarations need no registration — the chain metadata alone drives
/// <see cref="SecureWithFilter"/> at query time.
/// </summary>
public class SecureWithRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false })
            return;

        if (type.GetCustomAttribute<SecureWithAttribute>() == null)
            return;

        // Resolve = validate: throws with the exact defect named on misdeclaration.
        var chain = SecureWithChains.TryResolve(type)!;

        // Hop declaration — metadata only.
        if (chain.Hops.Count > 0)
            return;

        var entityKey = SecureWithChains.KeyOf(type)!;

        var provisioning = typeof(CreatorRoleProvisioningHandler<,,,>)
            .MakeGenericType(type, entityKey, chain.LinkType, chain.LinkKeyType);
        container.AddTransient(
            typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatedEvent<>).MakeGenericType(type)),
            provisioning);

        var validation = typeof(RoleableValidationHandler<>).MakeGenericType(chain.LinkType);
        container.AddTransient(
            typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatingEvent<>).MakeGenericType(chain.LinkType)),
            validation);
        container.AddTransient(
            typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityUpdatingEvent<>).MakeGenericType(chain.LinkType)),
            validation);
    }
}
