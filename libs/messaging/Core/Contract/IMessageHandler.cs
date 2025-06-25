
namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage? message);
}
