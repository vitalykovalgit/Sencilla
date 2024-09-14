namespace Sencilla.Component.Files;

public static class HandlerRouter
{
    public static ITusRequestHandler ResolveHandler(string method) =>
        method switch
        {
            //"POST" => new CreateFileHandler(new InMemoryFileStateStorage(), new DiskFileStorage()),
            //"PATCH" => new UploadFileHandler(new InMemoryFileStateStorage(), new DiskFileStorage()),
            _ => throw new NotImplementedException()
        };
}
