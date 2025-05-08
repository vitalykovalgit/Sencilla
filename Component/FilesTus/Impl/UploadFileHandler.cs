namespace Sencilla.Component.FilesTus;

[DisableInjection]
public class UploadFileHandler(
    IFileRepository fileRepository,
    IFileUploadRepository fileUploadRepository,
    IFileContentProvider fileContent,
    IEventDispatcher events)
    : ITusRequestHandler
{
    public const string Method = "PATCH";

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

        // create if not exists 
        var file = await fileRepository.GetFile(fileId) ?? await fileRepository.CreateFile(new() { Id = fileId, Origin = FileOrigin.User, StorageFileTypeId = fileContent.ProviderType });
        
        var newOffset = await fileContent.WriteFileAsync(file, chunk, offset, length, CancellationToken.None);

        // Update file upload status and notify the system 
        var fileUpload = await fileUploadRepository.GetFileUpload(fileId) ?? await fileUploadRepository.CreateFileUpload(new() { Id = fileId });
        fileUpload.Position = newOffset;
        fileUpload.UploadCompleted = fileUpload.Size == fileUpload.Position;
        fileUpload = await fileUploadRepository.UpdateFileUpload(fileUpload);
        if (fileUpload.UploadCompleted)
        {
            if(file.ParentId == null)
                await events.PublishAsync(new FileUploadedEvent { File = file, FileUpload = fileUpload });
            await fileUploadRepository.DeleteFileUpload(fileId);
        }

        await context.WriteNoContentWithOffset(newOffset);
    }
}
