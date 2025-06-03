
namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IMessageHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event);
}
