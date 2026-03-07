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

        // Check for resolution-specific progress
        var resParam = context.Request.Query[nameof(File.Res)].ToString();
        if (int.TryParse(resParam, out var res) && file?.Res != null)
        {
            var resKey = res.ToString();
            if (file.Res.TryGetValue(resKey, out var resInfo) && resInfo.S.HasValue)
            {
                context.WriteOkWithOffset(resInfo.S.Value, resInfo.U ?? 0);
                return;
            }
        }

        context.WriteOkWithOffset(file?.Size ?? 0, file?.Uploaded ?? 0);
    }
}
