
namespace Sencilla.Component.Files;

[DisableInjection]
internal class HeadFileHandler : ITusRequestHandler
{
    public const string Method = "HEAD";

    private readonly IFileProvider _fileState;

    public HeadFileHandler(IFileProvider fileState)
    {
        _fileState = fileState;
    }

    public async Task Handle(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(TusHeaders.TusResumable))
        {
            await context.WriteBadRequest($"{TusHeaders.TusResumable} header is missing.");
            return;
        }

        var segments = context.Request.Path.Value!.Split('/');
        var fileId = Guid.Parse(segments[segments.Length - 1]);

        var file = await _fileState.GetFile(fileId) ?? await _fileState.CreateFile(new() { Id = fileId });

        await context.WriteOkWithOffset(file.Position);
    }
}
