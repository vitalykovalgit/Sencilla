namespace Sencilla.Messaging;

/// <summary>
/// A payload that carries its initiating user — copied onto <see cref="Message.UserId"/> the same
/// way <see cref="IMessageHasEntity"/> feeds <see cref="Message.EntityId"/>, so the admin message
/// API and a failure handler in another process know whom it was for.
/// </summary>
public interface IMessageHasUser
{
    Guid? UserId { get; }
}
