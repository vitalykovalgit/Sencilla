﻿namespace Sencilla.Component.Files;

[DisableInjection]
internal class CreateFileHandler : ITusRequestHandler
{
    private readonly IFileStateProvider _fileState;
    private readonly IFileContentProvider _fileContent;

    public CreateFileHandler(IFileStateProvider state, IFileContentProvider fileContent)
    {
        _fileState = state;
        _fileContent = fileContent;
    }

    public async Task Handle(HttpContext context)
    {
        var fileId = Guid.NewGuid().ToString();

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

        var metadata = ParseMetadataHeader(metadataHeader);
        var fileName = FromBase64(metadata["filename"]);
        var fileMimeType = FromBase64(metadata["filetype"]);
        var fileExt = MimeTypeExt(fileMimeType);

        var file = await _fileState.SetFileState(new()
        {
            //Id = fileId,
            Name = fileName,
            Size = uploadLength,
            MimeType = fileMimeType,
            Extension = fileExt
        });

        // TODO: test cancellation token on middleware
        //       and test writing empty array to file
        await _fileContent.WriteFileAsync(file, []);
        //await _fileStorage.Create(fileId);

        // think about location for uploading, probably can be S3/CloudFare directly
        var location = $"{context.Request.Path.Value}/{fileId}";
        await context.WriteCreated(location);
    }

    //"filename cGV4ZWxzLWNvZHkta2luZy0xMTE4NjY3LmpwZw==,filetype aW1hZ2UvanBlZw=="
    private static IDictionary<string, string> ParseMetadataHeader(string metadataHeader) =>
        metadataHeader.Split(',').Select(x =>
        {
            var kv = x.Split(' ');
            return (key: kv[0], value: kv[1]);
        }).ToDictionary(kv => kv.key, kv => kv.value);

    private static string FromBase64(string b64) => Encoding.UTF8.GetString(Convert.FromBase64String(b64));

    private static string MimeTypeExt(string mimeType) => mimeType switch
    {
        MediaTypeNames.Image.Jpeg => ".jpg",
        _ => ".unknown"
    };
}
