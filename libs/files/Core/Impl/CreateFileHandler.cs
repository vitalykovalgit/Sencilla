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
        if (uploadLength < 0)
        {
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
}
