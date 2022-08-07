
namespace Sencilla.Core
{
    public interface IEventMiddleware
    {
        Task ProcessAsync<TEvent>(TEvent command) where TEvent : class, IEvent;
    }
}
