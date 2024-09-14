namespace Sencilla.Component.Files;

public interface ITusRequestHandler
{
    Task Handle(HttpContext context);
}
