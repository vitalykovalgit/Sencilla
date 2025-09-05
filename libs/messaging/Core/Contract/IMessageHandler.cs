
namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IMessageHandler<in TMessage>
{
    /// <summary>
    /// Handles the incoming message.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task HandleAsync(TMessage message);
}
