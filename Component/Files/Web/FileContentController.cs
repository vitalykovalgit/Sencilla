namespace Sencilla.Component.Files;

[Route("api/v1/files/stream")]
public class FileContentController : ApiController
{
    private readonly IResolver _resolver;
    private readonly IFileRepository _fileRepository;

    public FileContentController(
        IResolver resolver,
        IFileRepository fileRepository) : base(resolver)
    {
        _resolver = resolver;
        _fileRepository = fileRepository;
    }

    [HttpGet, Route("{fileId}")]
    public async Task<IActionResult> GetFileStream(
        [FromServices] IReadRepository<File, Guid> fileRepo,
        Guid fileId, int? dim, CancellationToken token)
    {
        var files = await fileRepo.GetAll(new FileFilter().ByOriginalId(fileId).ByDimmension(dim));
        var file = files.FirstOrDefault();
        if (file == null)
            return new NotFoundResult();

        var fileContentProvider = ResolveFileContentProvider(file);
        var stream = await fileContentProvider.ReadFileAsync(file, token);
        if (stream == null)
            return new NotFoundResult();

        //Response...
        //Response.Headers.Add("X-Content-Type-Options", "nosniff");
        //Response.Headers.Add("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Disposition", new System.Net.Mime.ContentDisposition
        {
            FileName = file.Name,
            //false = prompt the user for downloading;  
            //true = browser to try to show the file inline
            Inline = true
        }.ToString());

        return File(stream, file.MimeType ?? "", true);
    }

    [HttpGet, Route("{fileId}/stream")]
    public async Task<FileStreamResult> GetStream(Guid fileId, CancellationToken token)
    {
        var file = await _fileRepository.GetFile(fileId);
        //if (file == null)
        //    return NotFound();

        var fileContentProvider = ResolveFileContentProvider(file);

        var stream = await fileContentProvider.ReadFileAsync(file, token);
        //if (stream == null)
        //    return NotFound();

        // Response...
        //Response.Headers.Add("X-Content-Type-Options", "nosniff");
        //Response.Headers.Add("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Disposition", new System.Net.Mime.ContentDisposition
        {
            FileName = file.Name,
            //false = prompt the user for downloading;  
            //true = browser to try to show the file inline
            Inline = true
        }.ToString());

        //return new FileStreamResult(stream, file.MimeType);
        return File(stream, file.MimeType, true);
    }

    private IFileContentProvider ResolveFileContentProvider(File file) => _resolver.Resolve<IFileContentProvider>(IFileContentProvider.ServiceKey(file.StorageFileTypeId))!;
}
