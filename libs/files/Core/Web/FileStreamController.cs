
namespace Sencilla.Component.Files;

[DisableInjection]
[Route("api/v1/files/stream")]
public class FileStreamController(IServiceProvider provider, IReadRepository<File, Guid> fileRepo, IFilePathResolver pathResolver) : ApiController(provider)
{
    // TODO: Move to config cache duration
    [HttpGet, Route("{fileId}")]
    [ResponseCache(Duration = 172000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetFileStream(Guid fileId, int? dim, int? res, CancellationToken token)
    {
        // Resolution-based lookup
        if (res.HasValue)
        {
            var file = await fileRepo.GetById(fileId, token);
            if (file == null) return NotFound();

            if (file.Res == null || !file.Res.ContainsKey(res.Value.ToString()))
                return BadRequest($"File with resolution {res.Value} does not exist.");

            var resPath = pathResolver.GetResolutionPath(file, res.Value);
            var resFile = new File
            {
                Id = file.Id,
                Name = file.Name,
                MimeType = file.MimeType,
                Path = resPath,
                Storage = file.Storage
            };
            return await RetriveFileStream(resFile, token);
        }

        // Dimension-based lookup (existing behavior)
        var dimFile = await fileRepo.FirstOrDefault(new FileFilter().ByParentId(fileId).ByDimmension(dim), token);
        return await RetriveFileStream(dimFile, token);
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
