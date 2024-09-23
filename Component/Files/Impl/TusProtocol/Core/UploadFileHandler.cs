namespace Sencilla.Component.Files;

[DisableInjection]
internal class UploadFileHandler : ITusRequestHandler
{
    public const string Method = "PATCH";

    private readonly IFileProvider _fileState;
    private readonly IFileContentProvider _fileContent;

    public UploadFileHandler(IFileProvider fileState, IFileContentProvider fileContent)
    {
        _fileState = fileState;
        _fileContent = fileContent;
    }

    public async Task Handle(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(TusHeaders.UploadOffset))
        {
            await context.WriteBadRequest($"{TusHeaders.UploadOffset} header is missing.");
            return;
        }

        long offset = 0;
        if (context.Request.Headers.TryGetValue(TusHeaders.UploadOffset, out var offsetHeader) &&
            !long.TryParse(offsetHeader, out offset))
        {
            await context.WriteBadRequest($"Invalid {TusHeaders.UploadOffset} header.");
            return;
        }

        var segments = context.Request.Path.Value!.Split('/');
        var fileId = Guid.Parse(segments[segments.Length - 1]);
        var chunk = context.Request.Body;
        var length = (long)context.Request.ContentLength!;

        var file = await _fileState.GetFile(fileId) ?? await _fileState.CreateFile(new() { Id = fileId });

        var newOffset = await _fileContent.WriteFileAsync(file, chunk, offset, length, CancellationToken.None);

        file.Position = newOffset;
        file.UploadCompleted = file.Size == file.Position;
        await _fileState.UpdateFile(file);

        await context.WriteNoContentWithOffset(newOffset);
    }
}
