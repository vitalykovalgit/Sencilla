namespace Sencilla.Component.Files;

[DisableInjection]
internal class HeadFileHandler(IFileUploadRepository fileUploadRepository) : ITusRequestHandler
{
    public const string Method = "HEAD";

    public async Task Handle(HttpContext context, CancellationToken token)
    {
        if (!context.Request.Headers.ContainsKey(TusHeaders.TusResumable))
        {
            await context.WriteBadRequest($"{TusHeaders.TusResumable} header is missing.");
            return;
        }

        var segments = context.Request.Path.Value!.Split('/');
        var fileId = Guid.Parse(segments[segments.Length - 1]);

        var file = await fileUploadRepository.GetFileUpload(fileId) ?? await fileUploadRepository.CreateFileUpload(new() { Id = fileId });
        context.WriteOkWithOffset(file?.Position ?? 0);
    }
}
