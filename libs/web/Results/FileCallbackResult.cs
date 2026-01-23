namespace Sencilla.Web;

public class FileCallbackResult : FileResult
{
    Func<Stream, Task> Callback;

    public FileCallbackResult(string contentType, string fileDownloadName, Func<Stream, Task> callback) : base(contentType)
    {
        FileDownloadName = fileDownloadName;
        Callback = callback;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = ContentType.ToString();

        if (!string.IsNullOrEmpty(FileDownloadName))
            response.Headers["Content-Disposition"] = $"attachment; filename={FileDownloadName}";

        await Callback(response.Body);
    }
}
