namespace Sencilla.Component.Files;

public interface IFileRequestHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task Handle(HttpContext context, CancellationToken token);

    /// <summary>
    /// 
    /// </summary>
    static string ServiceKey(string method) => $"{nameof(IFileRequestHandler)}.{method}";
}
