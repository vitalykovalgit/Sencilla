
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandlerBase<in TEvent> where TEvent : class, IEvent
{
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandler<in TEvent>: IEventHandlerBase<TEvent> where TEvent : class, IEvent
{
    Task HandleAsync(TEvent @event);
}
