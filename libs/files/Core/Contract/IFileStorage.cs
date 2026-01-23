namespace Sencilla.Component.Files;

/// <summary>
/// Save file and retrieve to/from storage
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// 
    /// </summary>
    byte Type { get; }

    /// <summary>
    /// Retrieve directory from config
    /// from see FilesOptions.Dirs field 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="params"></param>
    /// <returns></returns>
    string GetDirectory(string type, params object[] @params);

    string GetRootDirectory();
    string GetUserDirectory<T>(T userId) => GetDirectory("User", userId!);

    Stream OpenFileStream(File file, long offset = 0, CancellationToken token = default);
    Stream OpenFileStream(string path, long offset = 0, CancellationToken token = default);

    Task<Stream?> ReadFileAsync(File file, CancellationToken token = default);
    Task<Stream?> ReadFileAsync(string file, CancellationToken token = default);

    Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken token = default);
    Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken token = default);

    Task ZipFolderAsync(string folderToArchive, string destinationFile, CancellationToken token = default);

    Task<File?> DeleteFileAsync(File? file, CancellationToken token = default);

    Task<bool> SaveFile(string file, Stream stream);
    Task<bool> SaveFile(string file, byte[] data);

    Task<bool> CopyToFileAsync(File srcFile, string dstPath);
    Task<bool> CopyToFileAsync(string srcPath, string dstPath);
}
