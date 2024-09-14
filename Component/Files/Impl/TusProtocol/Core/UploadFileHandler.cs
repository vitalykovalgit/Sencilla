namespace Sencilla.Component.Files;

[DisableInjection]
internal class UploadFileHandler : ITusRequestHandler
{
    private readonly IFileStateProvider _fileState;
    private readonly IFileContentProvider _fileContent;

    public UploadFileHandler(IFileStateProvider fileState, IFileContentProvider fileContent)
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
        var fileId = segments[segments.Length - 1];
        Stream chunk = context.Request.Body;

        var file = await _fileState.GetFileState(fileId);
        // fileState could be null, since CREATE is extension
        // so it's needed to handle both cases

        //var newOffset = await _fileContent.Write(fileId, chunk, offset);
        var newOffset = await _fileContent.WriteFileAsync(file, chunk, offset, CancellationToken.None);

        if (file?.Size == newOffset)
        {
            file.UploadCompleted = true;
            await _fileState.SetFileState(file);
            //await _fileContent.RestoreFile(fileId, file);
        }

        await context.WriteNoContentWithOffset(newOffset);
    }
}
