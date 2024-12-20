﻿namespace Sencilla.Component.FilesTus;

[DisableInjection]
public class UploadFileHandler : ITusRequestHandler
{
    public const string Method = "PATCH";

    private readonly IFileProvider _fileState;
    private readonly IFileContentProvider _fileContent;
    private readonly IEventDispatcher _events;

    public UploadFileHandler(IFileProvider fileState, IFileContentProvider fileContent, IEventDispatcher events)
    {
        _fileState = fileState;
        _fileContent = fileContent;
        _events = events;
    }

    public async Task Handle(TusContext context)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey(TusHeaders.UploadOffset))
        {
            await context.HttpContext.WriteBadRequest($"{TusHeaders.UploadOffset} header is missing.");
            return;
        }

        long offset = 0;
        if (context.HttpContext.Request.Headers.TryGetValue(TusHeaders.UploadOffset, out var offsetHeader) &&
            !long.TryParse(offsetHeader, out offset))
        {
            await context.HttpContext.WriteBadRequest($"Invalid {TusHeaders.UploadOffset} header.");
            return;
        }

        var segments = context.HttpContext.Request.Path.Value!.Split('/');
        var fileId = Guid.Parse(segments[segments.Length - 1]);
        var chunk = context.HttpContext.Request.Body;
        var length = (long)context.HttpContext.Request.ContentLength!;

        var file = await _fileState.GetFile(fileId) ?? await _fileState.CreateFile(new() { Id = fileId });

        var newOffset = await _fileContent.WriteFileAsync(file, chunk, offset, length, CancellationToken.None);

        file.Position = newOffset;
        file.UploadCompleted = file.Size == file.Position;
        await _fileState.UpdateFile(file);

        if (file.UploadCompleted)
            await _events.PublishAsync(new FileUploadedEvent { File = file });

        await context.HttpContext.WriteNoContentWithOffset(newOffset);
    }
}
