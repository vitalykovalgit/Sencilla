namespace Sencilla.Component.Files;

public static class TusRequestRouter
{
    public static Task Handle(IServiceProvider container, HttpContext context)
    {
        var handler = container
            .GetKeyedService<ITusRequestHandler>(ITusRequestHandler.ServiceKey(context.Request.Method))
            ?? throw new NotImplementedException();
        return handler.Handle(context);
    }
}
