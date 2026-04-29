namespace Sencilla.Component.Files;

[DisableInjection]
public class UploadFileHandler(
    IEventDispatcher events,
    IFileStorage fileStorage,
    IFilePathResolver pathResolver,
    IReadRepository<File, Guid> fileRepository,
    IUpdateRepository<FileUpload, Guid> fileUploadRepository,
    IUpdateRepository<File, Guid> fileUpdateRepository) : IFileRequestHandler
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

        // Check for resolution upload
        var resParam = context.Request.Query[nameof(File.Res)].ToString();
        if (int.TryParse(resParam, out var res))
        {
            await HandleResolutionUpload(context, file, res, offset, token);
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

    private async Task HandleResolutionUpload(HttpContext context, File file, int res, long offset, CancellationToken token)
    {
        var resKey = res.ToString();
        ResolutionInfo? resInfo = null;
        file.Res?.TryGetValue(resKey, out resInfo);
        if (resInfo == null)
        {
            await context.WriteBadRequest($"Resolution {res} is not registered for file {file.Id}");
            return;
        }

        // Write chunk to resolution file
        var resPath = pathResolver.GetResolutionPath(file, res);
        var resFile = new File { Path = resPath, Storage = file.Storage };
        var chunk = context.Request.Body;
        var length = (long)context.Request.ContentLength!;
        var newOffset = await fileStorage.WriteFileAsync(resFile, chunk, offset, length, token);

        // Update resolution upload progress (atomic JSON merge — no read-modify-write race)
        ResolutionInfo updatedInfo;
        if (resInfo.S.HasValue && newOffset >= resInfo.S.Value)
        {
            // Upload complete — clear size and uploaded
            updatedInfo = new ResolutionInfo();
            await events.PublishAsync(new FileUploadedEvent { File = file, Resolution = res }, token);
        }
        else
        {
            updatedInfo = new ResolutionInfo { S = resInfo.S, U = newOffset };
        }

        await fileUpdateRepository.JsonMergeAsync(file.Id, f => f.Res, resKey, updatedInfo, token);

        context.WriteNoContentWithOffset(newOffset);
    }
}
