namespace Sencilla.Component.FilesTus;

public interface ITusRequestHandler
{
    Task Handle(HttpContext context);

    static string ServiceKey(string method) => $"{nameof(ITusRequestHandler)}.{method}";
}
