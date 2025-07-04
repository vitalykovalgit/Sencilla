namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
public interface IEventDispatcher
{
    Task PublishAsync<T>(T @event) where T : class, IEvent;
}

