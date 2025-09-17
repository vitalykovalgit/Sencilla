// using Microsoft.Extensions.Options;

// namespace Sencilla.Component.Files;

// public class DbFileContentProvider : IFileStorage
// {
//     public string Type => "database";

//     public string GetDirectory(string type, params object[] @params) => throw new NotImplementedException();//=> options.GetDirectory(type, @params);

//     public string GetRootDirectory() => "/";
//     public Task<Stream> ReadFileAsync(File file, CancellationToken? token = null)
//     {
//         throw new System.NotImplementedException();
//     }

//     public Task<File?> DeleteFileAsync(File? file, CancellationToken? token = null)
//     {
//         throw new System.NotImplementedException();
//     }

//     public Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
//     {
//         throw new System.NotImplementedException();
//     }

//     public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
//     {
//         throw new System.NotImplementedException();
//     }

//     public Stream OpenFileStream(File file, long offset = 0, CancellationToken? token = null)
//     {
//         throw new NotImplementedException();
//     }

//     public Stream ReadFile(File file, CancellationToken? token = null)
//     {
//         throw new NotImplementedException();
//     }
// }
