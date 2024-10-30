namespace Sencilla.Component.FilesTus;

public interface ITusRequestHandler
{
    Task Handle(TusContext context);

    static string ServiceKey(string method) => $"{nameof(ITusRequestHandler)}.{method}";
}
