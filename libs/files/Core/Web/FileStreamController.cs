
namespace Sencilla.Component.Files;

[DisableInjection]
[Route("api/v1/files/stream")]
public class FileStreamController(IServiceProvider provider, IReadRepository<File, Guid> fileRepo) : ApiController(provider)
{
    // TODO: Move to config cache duration
    [HttpGet, Route("{fileId}")]
    [ResponseCache(Duration = 172000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetFileStream(Guid fileId, int? dim, CancellationToken token)
    {
        var file = await fileRepo.FirstOrDefault(new FileFilter().ByParentId(fileId).ByDimmension(dim), token);
        //var file = await fileRepo.GetById(Guid.Parse("E770032C-7478-4C74-AEFE-0003A646B894"));
        //var file = new File {
        //    Id = Guid.Parse("E770032C-7478-4C74-AEFE-0003A646B894"),
        //    Name = "DSC01494.jpg",
        //    MimeType = "image/jpeg",
        //    Path = "user1\\project31\\editors\\e770032c-7478-4c74-aefe-0003a646b894_1000px.jpg",
        //    Size = 147398,
        //    Uploaded = 147398,
        //    Origin = FileOrigin.User,
        //    Storage = 2,
        //    UserId = 1,
        //    Dim = 100,
        //};
        return await RetriveFileStream(file, token);
    }

    [HttpGet, Route("{fileId}/stream")]
    public async Task<IActionResult> GetStream(Guid fileId, CancellationToken token)
    {
        var file = await fileRepo.GetById(fileId);
        return await RetriveFileStream(file, token);
    }

    private async Task<IActionResult> RetriveFileStream(File? file, CancellationToken token) 
    {
        if (file == null) return NotFound();

        var storage = file.Storage == 0 ? provider.GetService<IFileStorage>() : provider.GetKeyedService<IFileStorage>(file.Storage);
        if (storage == null) return BadRequest($"File Storage is not configured for {file.Storage}");

        var stream = await storage.ReadFileAsync(file, token);
        if (stream == null) return NotFound();

        // Response...
        // With file name we have issue 
        // Invalid non-ASCII or control character in header: 0x03C3
        // So for now just replace with '_'
        var fileName = Regex.Replace(file.Name ?? "", @"[^\u0000-\u007F]+", "_");
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
        Response.Headers.Append("X-Content-Type-Options", "nosniff");
        Response.Headers.Append("Accept-Ranges", "bytes");
        return new FileStreamResult(stream, file.MimeType ?? "")
        {
            FileDownloadName = file.Name
        };
    }
}
