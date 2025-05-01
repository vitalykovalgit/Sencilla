using Sencilla.Component.FilesTus.Extension;

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

    public async Task Handle(HttpContext context)
    {
        var uploadMetadataExists = context.Request.Headers.ContainsKey(TusHeaders.UploadMetadata);
        var uploadLengthExists = context.Request.Headers.ContainsKey(TusHeaders.UploadLength);
        var uploadDeferLengthExists = context.Request.Headers.ContainsKey(TusHeaders.UploadDeferLength);

        // TODO: headers validation
        if (!uploadMetadataExists)
        {
            await context.WriteBadRequest($"{TusHeaders.UploadMetadata} header is missing.");
            return;
        }

        if (!uploadLengthExists && !uploadDeferLengthExists)
        {
            await context.WriteBadRequest($"{TusHeaders.UploadLength} or {TusHeaders.UploadLength} should be specified.");
            return;
        }

        if (context.Request.Headers.TryGetValue(TusHeaders.UploadDeferLength, out var uploadDeferLengthHeader) &&
            uploadDeferLengthHeader != "1")
        {
            await context.WriteBadRequest($"Invalid {TusHeaders.UploadDeferLength} value.");
            return;
        }

        long uploadLength = uploadLengthExists
            ? uploadLength = long.Parse(context.Request.Headers[TusHeaders.UploadLength])
            : -1;

        var metadataHeader = context.Request.Headers[TusHeaders.UploadMetadata];

        var metadata = metadataHeader.ToString()?.ParseMetadataHeader();
        var fileId = Guid.TryParse(metadata["id"], out var _fileId) ? _fileId : Guid.Empty;
        var fileName = metadata["filename"];
        var fileMimeType = metadata["filetype"];
        var fileExt = fileMimeType.MimeTypeExt();

        var file = await _fileRepository.CreateFile(new()
        {
            Id = fileId,
            Name = fileName,
            MimeType = fileMimeType,
            Extension = fileExt,
            Size = uploadLength,
            Origin = Enum.TryParse<FileOrigin>(metadata["fileOrigin"], out var origin) ? origin : FileOrigin.User,
            StorageFileTypeId = _fileContent.ProviderType,
            FullName = metadata["fullPath"]
            });
        
        var fileUpload = await _fileUploadRepository.CreateFileUpload(new()
        {
            Id = fileId,
            Size = uploadLength,
        });

        await _fileRepository.UpdateFile(file);
        if(file.Origin == FileOrigin.User) 
            await _events.PublishAsync(new FileCreatedEvent { File = file, Metadata = metadata });
        
        // TODO: test cancellation token on middleware
        //       and test writing empty array to file
        await _fileContent.WriteFileAsync(file, []);

        // think about location for uploading, probably can be S3/CloudFare directly
        var location = $"{context.Request.Path.Value}/{fileId}";
        await context.WriteCreated(location);
    }

    
}
