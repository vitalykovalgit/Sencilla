
namespace Sencilla.Messaging;

/// <summary>
/// Defines a middleware interface for processing messages in a messaging system.
/// </summary>
public interface IMessageMiddleware
{
    Task ProcessAsync<TMessage>(TMessage msg) where TMessage : class;
}