
using Sencilla.Component.Config;
using System.Globalization;

namespace Sencilla.Component.Files
{
    public class DriveFileProvider : IFileContentProvider
    {
        IConfigProvider<DriveFileProviderOption> mConfigProvider;

        public DriveFileProvider(IConfigProvider<DriveFileProviderOption> configProvider)
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

        public Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
        {
            var path = GetFilePath(file);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, content);
            
            return Task.FromResult(new FileInfo(path).Length);
        }

        public async Task<long> WriteFileAsync(File file, System.IO.Stream stream, long offset = 0, CancellationToken? token = null)
        {
            var path = GetFilePath(file);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

            long newOffset = 0;
            using (var fileStream = System.IO.File.OpenWrite(path))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream, token ?? CancellationToken.None);
                newOffset = fileStream.Position;
            }

            return newOffset;
        }

        private string GetFilePath(File file) 
        {
            var path = BuildFilePath(file);
            var config = mConfigProvider.GetConfig();
            return System.IO.Path.Combine(config.RootPath, path);
        }

        private string BuildFilePath(File file)
        {
            var year = file.CreatedDate.Year.ToString();
            var monthNumb = file.CreatedDate.Month.ToString();
            var monthName = file.CreatedDate.ToString("MMM", CultureInfo.InvariantCulture);
            var day = file.CreatedDate.Day.ToString();

            var fileExt = System.IO.Path.GetExtension(file.Name);
            //var fileName = Guid.NewGuid().ToString();
            var fileName = file.Id.ToString();

            var relativeFilePath = System.IO.Path.Combine(year, $"{monthNumb:00}_{monthName}", day, $"{fileName}{fileExt}");
            return relativeFilePath;
        }
    }
}
