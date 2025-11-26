namespace Sencilla.Component.Files;

[DisableInjection]
public class UploadFileHandler(
    IEventDispatcher events,
    IFileStorage fileStorage,
    IReadRepository<File, Guid> fileRepository,
    IUpdateRepository<FileUpload, Guid> fileUploadRepository) : IFileRequestHandler
{
    public const string Method = "PATCH";

    public async Task Handle(HttpContext context, CancellationToken token)
    {
        // Check upload offset 
        long offset = context.Request.Headers.GetLong(FileHeaders.UploadOffset, -1);
        if (offset < 0)
        {
            await context.WriteBadRequest($"{FileHeaders.UploadOffset} header is missing or invalid value.");
            return;
        }

        // Get file Id 
        var fileId = context.Request.Path.GetFileId();
        var file = await fileRepository.GetById(fileId);
        if (file == null) 
        {
            await context.WriteBadRequest($"File with id {fileId} does not exist");
            return;
        }

        // Write file to the storage 
        var chunk = context.Request.Body;
        var length = (long)context.Request.ContentLength!;        
        var newOffset = await fileStorage.WriteFileAsync(file, chunk, offset, length, token);

        // Update file upload status and notify the system 
        var fileUpload = await fileUploadRepository.Update(new FileUpload { Id = file.Id, Uploaded = newOffset });
        if (fileUpload!.Uploaded == file.Size)
            await events.PublishAsync(new FileUploadedEvent { File = file }, token);

        // Send response 
        context.WriteNoContentWithOffset(newOffset);
    }
}
