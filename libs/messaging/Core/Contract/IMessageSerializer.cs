namespace Sencilla.Messaging;

/// <summary>
/// Represents a queue or topic 
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    string Serialize<T>(Message<T> message);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    Message<T> Deserialize<T>(string message);
}
