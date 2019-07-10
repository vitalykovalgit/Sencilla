
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sencilla.Component.Config.Contract;

namespace Sencilla.Component.Files.Impl.ContentProvider
{
    public class DriveFileProvider : IFileContentProvider
    {
        IConfigProvider<DriveFileProviderOption> mConfigProvider;

        public DriveFileProvider(IConfigProvider<DriveFileProviderOption> configProvider)
        {
            mConfigProvider = configProvider;
        }

        public Task<Entity.File> DeleteFileAsync(Entity.File file, CancellationToken? token = null)
        {
            if (file != null)
            {
                var path = GetFilePath(file);
                System.IO.File.Delete(path);
            }

            return Task.FromResult(file);
        }

        public Task<Stream> ReadFileAsync(Entity.File file, CancellationToken? token = null)
        {
            var path = GetFilePath(file);
            
            Stream stream = null;
            if (File.Exists(path))
                stream = System.IO.File.OpenRead(path);
            
            return Task.FromResult(stream);
        }

        public Task<Entity.File> WriteFileAsync(Entity.File file, byte[] content, CancellationToken? token = null)
        {
            var path = GetFilePath(file);

            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, content);

            return Task.FromResult(file);
        }

        public async Task<Entity.File> WriteFileAsync(Entity.File file, Stream stream, CancellationToken? token = null)
        {
            var path = GetFilePath(file);

            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var fileStream = System.IO.File.OpenWrite(path))
            {
                await stream.CopyToAsync(fileStream, token ?? CancellationToken.None);
            }

            return file;
        }

        private string GetFilePath(Entity.File file) 
        {
            var path = BuildFilePath(file);
            var config = mConfigProvider.GetConfig();
            return Path.Combine(config.RootPath, path);
        }

        private string BuildFilePath(Entity.File file)
        {
            var year = file.CreatedDate.Year.ToString();
            var monthNumb = file.CreatedDate.Month.ToString();
            var monthName = file.CreatedDate.ToString("MMM", CultureInfo.InvariantCulture);
            var day = file.CreatedDate.Day.ToString();

            var fileExt = Path.GetExtension(file.Name);
            //var fileName = Guid.NewGuid().ToString();
            var fileName = file.Id.ToString();

            var relativeFilePath = Path.Combine(year, $"{monthNumb:00}_{monthName}", day, $"{fileName}{fileExt}");
            return relativeFilePath;
        }
    }
}
