namespace Sencilla.Component.FilesTus;

[DisableInjection]
internal class CreateFileHandler : ITusRequestHandler
{
    public const string Method = "POST";

    private readonly IFileRepository _fileRepository;
    private readonly IFileUploadRepository _fileUploadRepository;
    private readonly IFileContentProvider _fileContent;
    private readonly IEventDispatcher _events;

    public CreateFileHandler(
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

    public async Task Handle(TusContext context)
    {
        var fileId = Guid.NewGuid();

        var uploadMetadataExists = context.HttpContext.Request.Headers.ContainsKey(TusHeaders.UploadMetadata);
        var uploadLengthExists = context.HttpContext.Request.Headers.ContainsKey(TusHeaders.UploadLength);
        var uploadDeferLengthExists = context.HttpContext.Request.Headers.ContainsKey(TusHeaders.UploadDeferLength);

        // TODO: headers validation
        if (!uploadMetadataExists)
        {
            await context.HttpContext.WriteBadRequest($"{TusHeaders.UploadMetadata} header is missing.");
            return;
        }

        if (!uploadLengthExists && !uploadDeferLengthExists)
        {
            await context.HttpContext.WriteBadRequest($"{TusHeaders.UploadLength} or {TusHeaders.UploadLength} should be specified.");
            return;
        }

        if (context.HttpContext.Request.Headers.TryGetValue(TusHeaders.UploadDeferLength, out var uploadDeferLengthHeader) &&
            uploadDeferLengthHeader != "1")
        {
            await context.HttpContext.WriteBadRequest($"Invalid {TusHeaders.UploadDeferLength} value.");
            return;
        }

        long uploadLength = uploadLengthExists
            ? uploadLength = long.Parse(context.HttpContext.Request.Headers[TusHeaders.UploadLength])
            : -1;

        var metadataHeader = context.HttpContext.Request.Headers[TusHeaders.UploadMetadata];

        var metadata = ParseMetadataHeader(metadataHeader);
        var fileName = metadata["filename"];
        var fileMimeType = metadata["filetype"];
        var fileExt = MimeTypeExt(fileMimeType);

        var file = await _fileRepository.CreateFile(new()
        {
            Id = fileId,
            Name = fileName,
            MimeType = fileMimeType,
            Extension = fileExt,
            Origin = FileOrigin.User
        });

        var fileUpload = await _fileUploadRepository.CreateFileUpload(new()
        {
            Id = fileId,
            Size = uploadLength,
            StorageFileTypeId = _fileContent.ProviderType
        });

        await _events.PublishAsync(new FileCreatedEvent { File = file, Metadata = metadata });

        // TODO: test cancellation token on middleware
        //       and test writing empty array to file
        await _fileContent.WriteFileAsync(file, []);

        // think about location for uploading, probably can be S3/CloudFare directly
        var location = $"{context.HttpContext.Request.Path.Value}/{fileId}";
        await context.HttpContext.WriteCreated(location);
    }

    //"filename cGV4ZWxzLWNvZHkta2luZy0xMTE4NjY3LmpwZw==,filetype aW1hZ2UvanBlZw=="
    private static IDictionary<string, string> ParseMetadataHeader(string metadataHeader) =>
        metadataHeader.Split(',').Select(x =>
        {
            var kv = x.Split(' ');
            return (key: kv[0], value: kv[1]);
        }).ToImmutableDictionary(kv => kv.key, kv => FromBase64(kv.value));

    private static string FromBase64(string b64) => Encoding.UTF8.GetString(Convert.FromBase64String(b64));

    private static string? MimeTypeExt(string mimeType) => IFormFileEx.MimeTypeToExt[mimeType];
}
