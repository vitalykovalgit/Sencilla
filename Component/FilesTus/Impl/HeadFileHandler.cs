namespace Sencilla.Component.FilesTus;

[DisableInjection]
internal class HeadFileHandler : ITusRequestHandler
{
    public const string Method = "HEAD";

    private readonly IFileUploadRepository _fileUploadRepository;

    public HeadFileHandler(IFileUploadRepository fileUploadRepository)
    {
        _fileUploadRepository = fileUploadRepository;
    }

    public async Task Handle(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(TusHeaders.TusResumable))
        {
            await context.WriteBadRequest($"{TusHeaders.TusResumable} header is missing.");
            return;
        }

        var segments = context.Request.Path.Value!.Split('/');
        var fileId = Guid.Parse(segments[segments.Length - 1]);

        var file = await _fileUploadRepository.GetFileUpload(fileId) ?? await _fileUploadRepository.CreateFileUpload(new() { Id = fileId });

        await context.WriteOkWithOffset(file?.Position ?? 0);
    }
}
