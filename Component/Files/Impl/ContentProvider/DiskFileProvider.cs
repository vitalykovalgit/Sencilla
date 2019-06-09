
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sencilla.Component.Files.Entity;

namespace Sencilla.Component.Files.Impl.ContentProvider
{
    public class DiskFileProvider : IFileContentProvider
    {
        DiskFileProviderOption mOptions;

        public DiskFileProvider(DiskFileProviderOption options)
        {
            mOptions = options;
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
            Stream stream = System.IO.File.OpenRead(path);
            return Task.FromResult(stream);
        }

        public Task<Entity.File> WriteFileAsync(Entity.File file, byte[] content, CancellationToken? token = null)
        {
            file.Path = BuildFilePath(file);
            var path = GetFilePath(file);

            System.IO.File.WriteAllBytes(path, content);

            return Task.FromResult(file);
        }

        public Task<Entity.File> WriteFileAsync(Entity.File file, Stream stream, CancellationToken? token = null)
        {
            file.Path = BuildFilePath(file);
            var path = GetFilePath(file);

            using (var fileStream = System.IO.File.OpenWrite(path))
            {
                stream.CopyToAsync(fileStream, token ?? CancellationToken.None);
            }

            return Task.FromResult(file);
        }

        private string GetFilePath(Entity.File file) 
        {
            return Path.Combine(mOptions.RootPath, file.Path);
        }

        private string BuildFilePath(Entity.File file)
        {
            var year = file.CreatedDate.Year.ToString();
            var month = file.CreatedDate.Month.ToString();
            var day = file.CreatedDate.Day.ToString();

            var fileExt = Path.GetExtension(file.Name);
            var fileName = Guid.NewGuid().ToString();

            var relativeFilePath = Path.Combine(year, month, day, $"{fileName}{fileExt}");
            return relativeFilePath;
        }
    }
}
