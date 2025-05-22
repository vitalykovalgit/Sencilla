namespace Sencilla.Component.FilesTus;

[DisableInjection]
internal class CreateFileHandler : ITusRequestHandler
{
    public const string Method = "POST";

    private readonly IEventDispatcher _events;

    private readonly IFileRepository _fileRepository;
    private readonly IFileContentProvider _fileContent;
    private readonly IFileUploadRepository _fileUploadRepository;
    
    public CreateFileHandler(
        IEventDispatcher events,
        IFileRepository fileRepository,
        IFileContentProvider fileContent,
        IFileUploadRepository fileUploadRepository)
    {
        _events = events;
        _fileContent = fileContent;
        _fileRepository = fileRepository;
        _fileUploadRepository = fileUploadRepository;
    }

    public async Task Handle(HttpContext context)
    {
        // TODO: headers validation
        var uploadMetadataExists = context.Request.Headers.ContainsKey(TusHeaders.UploadMetadata);
        if (!uploadMetadataExists)
        {
            await context.WriteBadRequest($"{TusHeaders.UploadMetadata} header is missing.");
            return;
        }

        var uploadLengthExists = context.Request.Headers.ContainsKey(TusHeaders.UploadLength);
        var uploadDeferLengthExists = context.Request.Headers.ContainsKey(TusHeaders.UploadDeferLength);
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

        long uploadLength = uploadLengthExists ? long.Parse(context.Request.Headers[TusHeaders.UploadLength]) : -1;

        var metadataHeader = context.Request.Headers[TusHeaders.UploadMetadata];
        var metadata = metadataHeader.ToString().ParseMetadataHeader();
        //if (metadata == null)
        //{
        //    return;
        //}

        var fileOrigin = Enum.TryParse<FileOrigin>(metadata["fileOrigin"], out var origin) ? origin : FileOrigin.User;
        var fileName = metadata["filename"];
        var fileMimeType = metadata["filetype"];
        var fileExt = fileMimeType.MimeTypeExt();
        var fileId = Guid.NewGuid();
        var filePath = GetFullPath(metadata, fileExt, fileId, fileOrigin);

        var fileToCreate = metadata.TryGetValue("parentId", out var parentIdObj) && parentIdObj is string parentIdStr &&
            Guid.TryParse(parentIdStr, out Guid parentId)
                ? getResizerFile(fileId, parentId, filePath, fileMimeType, metadata)
                : new Files.File()
                {
                    Id = fileId,
                    Name = fileName,
                    MimeType = fileMimeType,
                    Extension = fileExt,
                    Size = uploadLength,
                    Origin = fileOrigin,
                    StorageFileTypeId = _fileContent.ProviderType,
                    FullName = filePath
                };

        if (int.TryParse(metadata["userId"], out var userId))
            fileToCreate.UserId = userId;

        var file = await _fileRepository.CreateFile(fileToCreate);
        var fileUpload = await _fileUploadRepository.CreateFileUpload(new()
        {
            Id = fileId,
            Size = uploadLength,
        });
        await _fileRepository.UpdateFile(file);

        if (file.Origin == FileOrigin.User && file.ParentId == null)
            await _events.PublishAsync(new FileCreatedEvent { File = file, Metadata = metadata });
        
        // TODO: test cancellation token on middleware
        //       and test writing empty array to file
        await _fileContent.WriteFileAsync(file, []);

        // think about location for uploading, probably can be S3/CloudFare directly
        var location = $"{context.Request.Path.Value}/{fileId}";
        await context.WriteCreated(location);
    }

    private Files.File getResizerFile(Guid fileId, Guid? parentId, string filePath, string? fileMimeType, IDictionary<string, string> metadata)
    {
        string folderName = metadata.TryGetValue("folderName", out var _folderName) ? _folderName : "compressed";
        int dim = metadata.TryGetValue("dim", out var _dim) && int.TryParse(_dim, out var dimValue) ? dimValue : 100;
        return new Files.File()
        {
            Id = fileId,
            ParentId = parentId,
            StorageFileTypeId = _fileContent.ProviderType,
            Dim = dim,
            Name = RescaledFileName(filePath, dim),
            FullName = filePath?.Substring(0, filePath.LastIndexOf('/') + 1) + RescaledFullName(filePath, folderName, dim),
            MimeType = fileMimeType,
            Origin = FileOrigin.System,
        };
    }

    private string GetFullPath(IDictionary<string, string> metadata, string fileExt, Guid fileId, FileOrigin origin)
        {
            var fileName = $"{fileId}{fileExt}";
            return origin switch
            {
                FileOrigin.User => Path.Combine($"user{metadata["userId"]}", $"project{metadata["projectId"]}", $"{fileName}"),
                FileOrigin.System => $"system/{fileName}",
                _ => $"none/{fileName}"
            };
        }
    private string RescaledFileName(string filePath, int size)
    {
        var pathParts = filePath.Split(Path.DirectorySeparatorChar);
        var fileName = pathParts[pathParts.Length - 1];

        return $"{Path.GetFileNameWithoutExtension(fileName)}_{size}px{Path.GetExtension(fileName)}";
    }

    private string RescaledFullName(string filePath, string directory, int size)
    {
        var pathParts = filePath.Split(Path.DirectorySeparatorChar);
        var fileName = pathParts[pathParts.Length - 1];
        var sb = new StringBuilder();

        for (int i = 0; i < pathParts.Length - 1; i++)
            sb.Append($"{pathParts[i]}{Path.DirectorySeparatorChar}");

        if (!string.IsNullOrEmpty(directory))
            sb.Append($"{directory}{Path.DirectorySeparatorChar}");
        sb.Append(RescaledFileName(filePath, size));

        return sb.ToString();
    }
}