namespace Sencilla.Component.Files;

[DisableInjection]
internal class CreateFileHandler(
    IFileStorage storage, 
    IEventDispatcher events,
    IFilePathResolver pathResolver,
    ICreateRepository<File, Guid> fileRepo): IFileRequestHandler
{
    public const string Method = "POST";

    public async Task Handle(HttpContext context, CancellationToken token)
    {
        var headers = context.Request.Headers;

        var uploadLength = headers.GetLong(FileHeaders.UploadLength, -1);
        if (uploadLength < 0) {
            await context.WriteBadRequest($"{FileHeaders.UploadLength} should be specified or more then 0.");
            return;
        }

        var uploadDeferLength = headers.GetLong(FileHeaders.UploadDeferLength, 1);
        if (uploadDeferLength != 1)
        {
            await context.WriteBadRequest($"{FileHeaders.UploadDeferLength} should be specified or has invalid value.");
            return;
        }

        var metadata = headers.GetMetadata();
        if (metadata == null)
        {
            await context.WriteBadRequest($"{FileHeaders.UploadMetadata} header is missing.");
            return;
        }

        var file = ToFile(metadata, uploadLength, out var missingHeaders);
        if (file == null) 
        {
            await context.WriteBadRequest($"{missingHeaders.Join(",")} keys are missing in metadata.");
            return;
        }

        // check if file exists use it otherways create it
        var dbFile = await fileRepo.GetById(file.Id);
        if (dbFile == null) 
        {
            dbFile = await fileRepo.Create(file);
            await events.PublishAsync(new FileCreatedEvent { File = dbFile }, token);
            // TODO: test cancellation token on middleware and test writing empty array to file
            await storage.WriteFileAsync(dbFile!, []);
        }

        // think about location for uploading, probably can be S3/CloudFare directly
        var location = $"{context.Request.Path.Value}/{dbFile!.Id}";
        context.WriteCreated(location);
    }

    private File? ToFile(IDictionary<string, string> metadata, long fileSize, out List<string> missingHeaders) 
    {
        missingHeaders = new List<string>();

        if (metadata == null) return null;

        // Get file name
        var fileName = metadata.GetString(nameof(File.Name));
        if (fileName == null) {
            missingHeaders.Add(nameof(File.Name));
            return null;
        }

        var file = new File
        {
            Id = metadata.GetGuid(nameof(File.Id)) ?? Guid.NewGuid(),
            Name = fileName,
            Size = metadata.GetLong(nameof(File.Size)) ?? fileSize,
            MimeType = metadata.GetString(nameof(File.MimeType)) ?? MimeTypeMap.GetMimeType(fileName),

            Dim = metadata.GetInt(nameof(File.Dim)),
            Origin = metadata.GetEnum<FileOrigin>(nameof(File.Origin)) ?? FileOrigin.User,
            Storage = storage.Type,
            ParentId = metadata.GetGuid(nameof(File.ParentId)),
            UserId = metadata.GetInt(nameof(File.UserId)),
            Width = metadata.GetInt(nameof(File.Width)),
            Height = metadata.GetInt(nameof(File.Height)),

            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,

            Attrs = metadata.RemoveKeys(
                nameof(File.Id),
                nameof(File.Dim),
                nameof(File.Name),
                nameof(File.Size),
                nameof(File.Width), 
                nameof(File.Height), 
                nameof(File.Origin),
                nameof(File.UserId),
                nameof(File.MimeType),
                nameof(File.ParentId)
            )
        };
        file.Path = pathResolver.GetFullPath(file);
        return file;
    }

    //var fileOrigin = Enum.TryParse<FileOrigin>(metadata["fileOrigin"], out var origin) ? origin : FileOrigin.User;
    //var fileName = metadata["filename"];
    //var fileMimeType = metadata["filetype"];
    //var fileExt = IFormFileEx.MimeTypeToExt[fileMimeType];
    //var fileId = Guid.NewGuid();
    //var filePath = GetFullPath(metadata, fileExt, fileId, fileOrigin);

    //var fileToCreate = metadata.TryGetValue("parentId", out var parentIdObj) && parentIdObj is string parentIdStr &&
    //    Guid.TryParse(parentIdStr, out Guid parentId)
    //        ? getResizerFile(fileId, parentId, filePath, fileMimeType, metadata)
    //        : new Files.File()
    //        {
    //            Id = fileId,
    //            Name = fileName,
    //            MimeType = fileMimeType,
    //            Size = uploadLength,
    //            Origin = fileOrigin,
    //            Storage = fileStorage.Type,
    //            Path = filePath
    //        };

    //if (int.TryParse(metadata["userId"], out var userId))
    //    fileToCreate.UserId = userId;


    //private Files.File getResizerFile(Guid fileId, Guid? parentId, string filePath, string? fileMimeType, IDictionary<string, string> metadata)
    //{
    //    string folderName = metadata.TryGetValue("folderName", out var _folderName) ? _folderName : "compressed";
    //    int dim = metadata.TryGetValue("dim", out var _dim) && int.TryParse(_dim, out var dimValue) ? dimValue : 100;
    //    return new Files.File()
    //    {
    //        Id = fileId,
    //        ParentId = parentId,
    //        Storage = fileStorage.Type,
    //        Dim = dim,
    //        Name = RescaledFileName(filePath, dim),
    //        Path = filePath?.Substring(0, filePath.LastIndexOf('/') + 1) + RescaledFullName(filePath, folderName, dim),
    //        MimeType = fileMimeType,
    //        Origin = FileOrigin.System,
    //    };
    //}

    //private string GetFullPath(IDictionary<string, string> metadata, string fileExt, Guid fileId, FileOrigin origin)
    //    {
    //        var fileName = $"{fileId}{fileExt}";
    //        return origin switch
    //        {
    //            FileOrigin.User => Path.Combine($"user{metadata["userId"]}", $"project{metadata["projectId"]}", $"{fileName}"),
    //            FileOrigin.System => $"system/{fileName}",
    //            _ => $"none/{fileName}"
    //        };
    //    }
    //private string RescaledFileName(string filePath, int size)
    //{
    //    var pathParts = filePath.Split(Path.DirectorySeparatorChar);
    //    var fileName = pathParts[pathParts.Length - 1];

    //    return $"{Path.GetFileNameWithoutExtension(fileName)}_{size}px{Path.GetExtension(fileName)}";
    //}

    //private string RescaledFullName(string filePath, string directory, int size)
    //{
    //    var pathParts = filePath.Split(Path.DirectorySeparatorChar);
    //    var fileName = pathParts[pathParts.Length - 1];
    //    var sb = new StringBuilder();

    //    for (int i = 0; i < pathParts.Length - 1; i++)
    //        sb.Append($"{pathParts[i]}{Path.DirectorySeparatorChar}");

    //    if (!string.IsNullOrEmpty(directory))
    //        sb.Append($"{directory}{Path.DirectorySeparatorChar}");
    //    sb.Append(RescaledFileName(filePath, size));

    //    return sb.ToString();
    //}
}