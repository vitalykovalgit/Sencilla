namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// The [Message] row as the web pipeline sees it: the entity <see cref="IMessageQueue"/> inserts
/// through the app's own DbContext, so an enqueue commits inside the caller's transaction. Also
/// the admin read surface — every failed message is queryable from day one (matrix resource
/// 'appmessage'; grant READ only, the queue is driven by code).
///
/// Named AppMessage rather than Message to avoid clashing with the <see cref="Message"/> envelope;
/// the table keeps the plain name. Columns live on <see cref="MessageRow"/>; the worker's twin over
/// the same table is <see cref="QueueMessage"/>.
/// </summary>
[Table("Message")]
[CrudApi("api/v1/messaging/messages")]
public class AppMessage : MessageRow;
