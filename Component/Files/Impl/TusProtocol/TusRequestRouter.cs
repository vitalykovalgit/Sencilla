namespace Sencilla.Component.Files;

public static class TusRequestRouter
{
    public static Task Handle(IServiceProvider container, HttpContext context)
    {
        ITusRequestHandler? handler = context.Request.Method switch
        {
            "POST" => container.GetKeyedService<ITusRequestHandler>(nameof(CreateFileHandler)),
            "PATCH" => container.GetKeyedService<ITusRequestHandler>(nameof(UploadFileHandler)),
            _ => null
        } ?? throw new NotImplementedException();

        return handler.Handle(context);
    }
}
