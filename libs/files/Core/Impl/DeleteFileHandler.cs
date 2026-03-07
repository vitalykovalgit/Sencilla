namespace Sencilla.Component.Files;

[DisableInjection]
internal class DeleteFileHandler(
    IFileStorage storage,
    IEventDispatcher events,
    IFilePathResolver pathResolver,
    IReadRepository<File, Guid> fileReadRepo,
    IDeleteRepository<File, Guid> fileDeleteRepo) : IFileRequestHandler
{
    public const string Method = "DELETE";

    public async Task Handle(HttpContext context, CancellationToken token)
    {
        var fileId = context.Request.Path.GetFileId();
        var file = await fileReadRepo.GetById(fileId);
        if (file == null)
        {
            await context.WriteBadRequest($"File with id {fileId} does not exist.");
            return;
        }

        // Find and delete all dimension variants
        var variants = await fileReadRepo.GetAll(new FileFilter().ByParentId(fileId), token);
        foreach (var variant in variants)
        {
            await storage.DeleteFileAsync(variant, token);
            await fileDeleteRepo.Delete(variant, token);
        }

        // Delete all resolution files from storage
        if (file.Res != null)
        {
            foreach (var resKey in file.Res.Keys)
            {
                if (int.TryParse(resKey, out var res))
                {
                    var resPath = pathResolver.GetResolutionPath(file, res);
                    await storage.DeleteFileAsync(resPath, token);
                }
            }
        }

        // Delete the original file
        await storage.DeleteFileAsync(file, token);
        await fileDeleteRepo.Delete(file, token);

        await events.PublishAsync(new FileDeletedEvent { File = file }, token);

        context.Response.StatusCode = 204;
    }
}
