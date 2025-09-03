namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
public interface IEventDispatcher
{
    Task PublishAsync<T>(T @event, CancellationToken token) where T : class, IEvent;
}

