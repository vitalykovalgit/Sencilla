namespace Sencilla.Component.Files;

[DisableInjection]
internal class HeadFileHandler(IReadRepository<File, Guid> fileRepo) : IFileRequestHandler
{
    public const string Method = "HEAD";

    public async Task Handle(HttpContext context, CancellationToken token)
    {
        if (!context.Request.Headers.ContainsKey(FileHeaders.TusResumable))
        {
            await context.WriteBadRequest($"{FileHeaders.TusResumable} header is missing.");
            return;
        }

        var fileId = context.Request.Path.GetFileId();
        var file = await fileRepo.GetById(fileId);

        context.WriteOkWithOffset(file?.Uploaded ?? 0);
    }
}
