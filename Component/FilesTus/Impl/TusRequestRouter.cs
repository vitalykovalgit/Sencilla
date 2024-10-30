namespace Sencilla.Component.FilesTus;

public static class TusRequestRouter
{
    public static Task Handle(IServiceProvider container, TusContext context)
    {
        var handler = container
            .GetKeyedService<ITusRequestHandler>(ITusRequestHandler.ServiceKey(context.HttpContext.Request.Method))
            ?? throw new NotImplementedException();
        return handler.Handle(context);
    }
}
