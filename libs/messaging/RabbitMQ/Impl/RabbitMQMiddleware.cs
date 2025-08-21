// namespace Sencilla.Messaging.RabbitMQ;

// /// <summary>
// /// RabbitMQ middleware for the Sencilla messaging system
// /// </summary>
// public class RabbitMQMiddleware : IMessageMiddleware
// {
//     readonly IRabbitMQPublisher _publisher;
//     readonly ILogger<RabbitMQMiddleware> _logger;

//     public RabbitMQMiddleware(IRabbitMQPublisher publisher, ILogger<RabbitMQMiddleware> logger)
//     {
//         _publisher = publisher;
//         _logger = logger;
//     }

//     public async Task ProcessAsync<T>(Message<T>? msg)
//     {
//         try
//         {
//             // Check if message has routing attributes or use default routing
//             var messageType = typeof(T);
//             var streamAttribute = messageType.GetCustomAttribute<StreamAttribute>();
//             //var topicAttribute = messageType.GetCustomAttribute<TopicAttribute>();

//             if (streamAttribute != null)
//             {
//                 streamAttribute.Names.ForEach(async name =>
//                 {
//                     await _publisher.PublishToQueueAsync(msg, name);
//                 });
//                 //_logger.LogDebug("Published message {MessageType} to queue {QueueName}", messageType.Name, queueAttribute.Name);
//             }
//             // else if (topicAttribute != null)
//             // {
//             //     await _publisher.PublishAsync(msg, topicAttribute.Exchange, topicAttribute.RoutingKey);
//             //     _logger.LogDebug("Published message {MessageType} to exchange {Exchange} with routing key {RoutingKey}", 
//             //         messageType.Name, topicAttribute.Exchange, topicAttribute.RoutingKey);
//             // }
//             else
//             {
//                 // Default behavior: publish to queue named after the message type
//                 var queueName = messageType.Name.ToLowerInvariant();
//                 await _publisher.PublishToQueueAsync(msg, queueName);
//                 _logger.LogDebug("Published message {MessageType} to default queue {QueueName}", 
//                     messageType.Name, queueName);
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Failed to process message {MessageType} through RabbitMQ middleware", typeof(T).Name);
//             throw;
//         }
//     }
// }
