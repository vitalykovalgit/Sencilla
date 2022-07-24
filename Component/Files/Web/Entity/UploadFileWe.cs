using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Files
{
    public class UploadFileWe
    {
        public IFormFile File { get; set; }
    }

    public class UploadFilesWe
    {
        public IFormFileCollection Files { get; set; }
    }
}
