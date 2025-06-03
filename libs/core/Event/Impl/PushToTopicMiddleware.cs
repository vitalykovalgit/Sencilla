
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
//public class PushToTopicMiddleware : IEventMiddleware
//{
//    readonly IMessagingService MessageService;

//    public PushToTopicMiddleware(IMessagingService messageService)
//    {
//        MessageService = messageService;
//    }
//    public async Task ProcessAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
//    {
//        // get topic attribute
//        var topicAttr = (TopicAttribute?)typeof(TEvent).GetCustomAttributes(typeof(TopicAttribute), false).FirstOrDefault();
//        if (topicAttr != null)
//        {
//            await MessageService.SendToTopicAsync(@event, topicAttr.Name, topicAttr.ConnectionString);
//        }
//    }
//}
