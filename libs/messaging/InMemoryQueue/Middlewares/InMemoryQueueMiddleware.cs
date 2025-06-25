namespace Sencilla.Messaging.InMemoryQueue;

[DisableInjection]
public class InMemoryQueueMiddleware(MessagingConfig config, InMemoryMessageBroker broker): IMessageMiddleware
{
    public Task ProcessAsync<T>(Message<T>? msg)
    {
        // Run middlewares 
        //config.Middlewares?.ForEach(async middleware =>
        //{
        //var middlewareInstance = (IMessageMiddleware)ActivatorUtilities.CreateInstance(config.ServiceProvider, middleware);
        //await middlewareInstance.ProcessAsync(msg);
        //});

        // Get routing information from the message
        // var route = config.Routing.GetRoute<T>();
        // route.Streams?.ForEach(async name =>
        // {
        //     //var queue = broker.GetOrCreateQueue<Message<T>>(name);
        //     //await queue.EnqueueAsync(msg);
        //     var streamConfig = config.Streams[name];
        // });
        config.Routing.GetRoute<T>().Streams?.Select(s => config.Streams[s].Providers);

        //Console.WriteLine($"InMemoryQueueMiddleware: Processing message of type {typeof(T).Name}");
        return Task.CompletedTask;
    }
}
