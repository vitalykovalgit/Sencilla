using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Component.Files;

[DisableInjection]
[Route("api/v1/files/stream")]
public class FileStreamController(IServiceProvider provider, IReadRepository<File, Guid> fileRepo) : ApiController(provider)
{
    [HttpGet, Route("{fileId}")]
    public async Task<IActionResult> GetFileStream(Guid fileId, int? dim, CancellationToken token)
    {
        var file = await fileRepo.FirstOrDefault(new FileFilter().ByParentId(fileId).ByDimmension(dim));
        return await RetriveFileStream(file, token);
    }

    [HttpGet, Route("{fileId}/stream")]
    //public async Task<FileStreamResult> GetStream(Guid fileId, CancellationToken token)
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
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{file.Name}\"");
        Response.Headers.Append("X-Content-Type-Options", "nosniff");
        Response.Headers.Append("Accept-Ranges", "bytes");
        return new FileStreamResult(stream, file.MimeType ?? "")
        {
            FileDownloadName = file.Name
        };
    }
}
