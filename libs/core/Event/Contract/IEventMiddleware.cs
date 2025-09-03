
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
public interface IEventMiddleware
{
    Task ProcessAsync<TEvent>(TEvent command, CancellationToken token) where TEvent : class, IEvent;
}