namespace Sencilla.Component.Files;

public interface IFileRepository
{
    Task<File?> CreateFile(File file);
    Task<File?> GetFile(Guid fileId);
    Task<File?> UpdateFile(File file);
}
