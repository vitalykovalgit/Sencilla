namespace Sencilla.Component.Files;

public class DriveFileContentProvider : IFileContentProvider
{
    IConfigProvider<DriveFileContentProviderOption> mConfigProvider;

    public DriveFileContentProvider(IConfigProvider<DriveFileContentProviderOption> configProvider)
    {
        mConfigProvider = configProvider;
    }

    public Task<File> DeleteFileAsync(File file, CancellationToken? token = null)
    {
        if (file != null)
        {
            var path = GetFilePath(file);
            System.IO.File.Delete(path);
        }

        return Task.FromResult(file);
    }

    public Task<System.IO.Stream> ReadFileAsync(File file, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        System.IO.Stream stream = null;
        if (System.IO.File.Exists(path))
            stream = System.IO.File.OpenRead(path);

        return Task.FromResult(stream);
    }

    public async Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        await System.IO.File.WriteAllBytesAsync(path, content);

        return new FileInfo(path).Length;
    }

    public async Task<long> WriteFileAsync(File file, System.IO.Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        fs.Seek(offset, SeekOrigin.Begin);

        //if (length >= 0)
        //{
        //    const int bufferSize = 81920; // 80KB
        //    var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        //    try
        //    {
        //        int bytesRead;
        //        while ((bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        //            await fs.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
        //    }
        //    finally
        //    {
        //        ArrayPool<byte>.Shared.Return(buffer);
        //    }
        //}
        //else
        //{
        //    await stream.CopyToAsync(fs, token ?? CancellationToken.None);
        //}
        await stream.CopyToAsync(fs, token ?? CancellationToken.None);

        long newOffset = fs.Position;

        return newOffset;
    }

    private string GetFilePath(File file)
    {
        var path = file.Id.ToString();
        return Path.Combine("uploaded-files", path);
        //var path = BuildFilePath(file);
        //var config = mConfigProvider.GetConfig();
        //return System.IO.Path.Combine(config.RootPath, path);
    }

    //private string BuildFilePath(File file)
    //{
    //    var year = file.CreatedDate.Year.ToString();
    //    var monthNumb = file.CreatedDate.Month.ToString();
    //    var monthName = file.CreatedDate.ToString("MMM", CultureInfo.InvariantCulture);
    //    var day = file.CreatedDate.Day.ToString();

    //    var fileExt = System.IO.Path.GetExtension(file.Name);
    //    //var fileName = Guid.NewGuid().ToString();
    //    var fileName = file.Id.ToString();

    //    var relativeFilePath = System.IO.Path.Combine(year, $"{monthNumb:00}_{monthName}", day, $"{fileName}{fileExt}");
    //    return relativeFilePath;
    //}
}
