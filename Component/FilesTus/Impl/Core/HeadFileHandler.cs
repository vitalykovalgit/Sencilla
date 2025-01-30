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

    public async Task Handle(TusContext context)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey(TusHeaders.TusResumable))
        {
            await context.HttpContext.WriteBadRequest($"{TusHeaders.TusResumable} header is missing.");
            return;
        }

        var segments = context.HttpContext.Request.Path.Value!.Split('/');
        var fileId = Guid.Parse(segments[segments.Length - 1]);

        var file = await _fileUploadRepository.GetFileUpload(fileId) ?? await _fileUploadRepository.CreateFileUpload(new() { Id = fileId });

        await context.HttpContext.WriteOkWithOffset(file.Position);
    }
}
