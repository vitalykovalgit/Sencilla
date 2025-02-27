namespace Sencilla.Component.FilesTus;

static class TusHeaders
{
    public const string TusResumable = "Tus-Resumable";
    public const string TusVersion = "Tus-Version";
    public const string TusMaxSize = "Tus-Max-Size";
    public const string TusExtension = "Tus-Extension";

    public const string UploadOffset = "Upload-Offset";
    public const string UploadLength = "Upload-Length";
    public const string UploadDeferLength = "Upload-Defer-Length";
    public const string UploadMetadata = "Upload-Metadata";
}
