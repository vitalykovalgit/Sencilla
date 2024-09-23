namespace Sencilla.Component.Files;

internal static class HttpContextExt
{
    public static Task WriteBadRequest(this HttpContext context, string message)
    {
        context.Response.ContentType = MediaTypeNames.Text.Plain;
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync(message);
    }

    public async static Task WriteCreated(this HttpContext context, string locationHeader)
    {
        context.Response.Headers.Append("Location", locationHeader);
        context.Response.StatusCode = StatusCodes.Status201Created;
    }

    public async static Task WriteNoContentWithOffset(this HttpContext context, long offset)
    {
        context.Response.Headers.Append(TusHeaders.UploadOffset, offset.ToString());
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    public async static Task WriteOkWithOffset(this HttpContext context, long offset)
    {
        context.Response.Headers.Append(TusHeaders.UploadOffset, offset.ToString());
        context.Response.StatusCode = StatusCodes.Status200OK;
    }
}
