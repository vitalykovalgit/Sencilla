namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 
    /// </summary>
    Command,
    
    /// <summary>
    /// 
    /// </summary>
    Event,
    
    /// <summary>
    /// 
    /// </summary>
    Query,
    
    // TODO: Maybe move to separate state
    Request,
    Response,
}