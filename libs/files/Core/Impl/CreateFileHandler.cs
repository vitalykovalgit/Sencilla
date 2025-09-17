namespace Sencilla.Component.Files;

[DisableInjection]
internal class CreateFileHandler(
    IEventDispatcher events,
    IFileRepository fileRepository,
    IFileStorage fileStorage,
    IFileUploadRepository fileUploadRepository) : ITusRequestHandler
{
    public const string Method = "POST";

    public async Task Handle(HttpContext context, CancellationToken token)
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
                    Size = uploadLength,
                    Origin = fileOrigin,
                    Storage = fileStorage.Type,
                    Path = filePath
                };

        if (int.TryParse(metadata["userId"], out var userId))
            fileToCreate.UserId = userId;

        var file = await fileRepository.CreateFile(fileToCreate);
        var fileUpload = await fileUploadRepository.CreateFileUpload(new()
        {
            Id = fileId,
            Size = uploadLength,
        });
        await fileRepository.UpdateFile(file);

        if (file.Origin == FileOrigin.User && file.ParentId == null)
            await events.PublishAsync(new FileCreatedEvent { File = file, Metadata = metadata }, token);
        
        // TODO: test cancellation token on middleware
        //       and test writing empty array to file
        await fileStorage.WriteFileAsync(file, []);

        // think about location for uploading, probably can be S3/CloudFare directly
        var location = $"{context.Request.Path.Value}/{fileId}";
        context.WriteCreated(location);
    }

    private Files.File getResizerFile(Guid fileId, Guid? parentId, string filePath, string? fileMimeType, IDictionary<string, string> metadata)
    {
        string folderName = metadata.TryGetValue("folderName", out var _folderName) ? _folderName : "compressed";
        int dim = metadata.TryGetValue("dim", out var _dim) && int.TryParse(_dim, out var dimValue) ? dimValue : 100;
        return new Files.File()
        {
            Id = fileId,
            ParentId = parentId,
            Storage = fileStorage.Type,
            Dim = dim,
            Name = RescaledFileName(filePath, dim),
            Path = filePath?.Substring(0, filePath.LastIndexOf('/') + 1) + RescaledFullName(filePath, folderName, dim),
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