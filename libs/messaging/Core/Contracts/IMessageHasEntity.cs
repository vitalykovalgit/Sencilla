namespace Sencilla.Messaging;

/// <summary>
/// A payload that names the entity it is about. The dispatcher copies it onto
/// <see cref="Message.EntityId"/>, which durable transports store as an indexed column, so a sender
/// can write <c>dapp.CloneProject(...)</c> and still let anyone ask "is anything queued for this
/// entity?" without building an envelope by hand. Implement explicitly when the value is derived
/// from another property, so the generated dispatcher extension does not grow a parameter for it.
/// </summary>
public interface IMessageHasEntity
{
    Guid? EntityId { get; }
}
