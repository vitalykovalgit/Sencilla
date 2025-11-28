namespace Microsoft.AspNetCore.Http;

[DisableInjection]
internal static class HttpContextExt
{
    public static Task WriteBadRequest(this HttpContext context, string message)
    {
        context.Response.ContentType = MediaTypeNames.Text.Plain;
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync(message);
    }

    public static void WriteCreated(this HttpContext context, string locationHeader)
    {
        context.Response.Headers.Append("Location", locationHeader);
        context.Response.StatusCode = StatusCodes.Status201Created;
    }

    public static void WriteNoContentWithOffset(this HttpContext context, long offset)
    {
        context.Response.Headers.Append(FileHeaders.UploadOffset, offset.ToString());
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    public static void WriteOkWithOffset(this HttpContext context, long length, long offset)
    {
        context.Response.Headers.Append(FileHeaders.UploadLength, length.ToString());
        context.Response.Headers.Append(FileHeaders.UploadOffset, offset.ToString());
        context.Response.StatusCode = StatusCodes.Status200OK;
    }
}
