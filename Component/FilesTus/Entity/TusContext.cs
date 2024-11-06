namespace Sencilla.Component.FilesTus;

public class TusContext
{
    public TusResumableUploadOptions Configuration { get; set; }

    public HttpContext HttpContext { get; set; }
}
