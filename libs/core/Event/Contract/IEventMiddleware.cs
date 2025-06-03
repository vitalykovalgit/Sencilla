
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
public interface IEventMiddleware
{
    Task ProcessAsync<TEvent>(TEvent command) where TEvent : class, IEvent;
}