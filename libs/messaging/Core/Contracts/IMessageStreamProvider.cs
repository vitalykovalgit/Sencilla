namespace Sencilla.Messaging;

public interface IMessageStreamProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IMessageStream? GetStream(StreamConfig config);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IMessageStream GetOrCreateStream(StreamConfig config);

}
