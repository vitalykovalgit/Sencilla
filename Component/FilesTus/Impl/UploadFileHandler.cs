namespace Sencilla.Component.FilesTus;

[DisableInjection]
public class UploadFileHandler : ITusRequestHandler
{
    public const string Method = "PATCH";

    private readonly IFileRepository _fileRepository;
    private readonly IFileUploadRepository _fileUploadRepository;
    private readonly IFileContentProvider _fileContent;
    private readonly IEventDispatcher _events;

    public UploadFileHandler(
        IFileRepository fileRepository,
        IFileUploadRepository fileUploadRepository,
        IFileContentProvider fileContent,
        IEventDispatcher events)
    {
        _fileRepository = fileRepository;
        _fileUploadRepository = fileUploadRepository;
        _fileContent = fileContent;
        _events = events;
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

        var file = await _fileRepository.GetFile(fileId) ?? await _fileRepository.CreateFile(new() { Id = fileId, Origin = FileOrigin.User, StorageFileTypeId = _fileContent.ProviderType });
        var fileUpload = await _fileUploadRepository.GetFileUpload(fileId) ?? await _fileUploadRepository.CreateFileUpload(new() { Id = fileId });
        var newOffset = await _fileContent.WriteFileAsync(file, chunk, offset, length, CancellationToken.None);

        fileUpload.Position = newOffset;
        fileUpload.UploadCompleted = fileUpload.Size == fileUpload.Position;
        fileUpload = await _fileUploadRepository.UpdateFileUpload(fileUpload);

        if (fileUpload.UploadCompleted)
        {
            await _events.PublishAsync(new FileUploadedEvent { File = file, FileUpload = fileUpload });
            await _fileUploadRepository.DeleteFileUpload(fileId);
        }

        await context.WriteNoContentWithOffset(newOffset);
    }
}
