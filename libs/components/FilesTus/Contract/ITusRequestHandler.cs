namespace Sencilla.Component.FilesTus;

public interface ITusRequestHandler
{
    Task Handle(HttpContext context, CancellationToken token);

    static string ServiceKey(string method) => $"{nameof(ITusRequestHandler)}.{method}";
}
